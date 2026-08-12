using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class AccountLifecycleIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNameCaseInsensitive = true
        };

    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly TestEmailSender _emailSender;

    public AccountLifecycleIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _emailSender = factory.Services
            .GetRequiredService<TestEmailSender>();
        _emailSender.Clear();
    }

    [Fact]
    public async Task InviteUser_ByAdmin_CreatesPendingAccountWithoutExposingToken()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var departmentId = await GetDepartmentIdAsync();
        var email = UniqueEmail("invited");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/users/invitations",
            adminToken,
            new InviteUserRequest(
                "Invited Technician",
                email,
                UserRole.Technician,
                departmentId));
        request.Headers.Host = "attacker-controlled.example";

        using var response = await _client.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var user = Deserialize<UserDto>(responseBody);
        Assert.Equal("PendingInvitation", user.AccountStatus);

        var emailMessage = Assert.Single(
            _emailSender.Messages,
            message => message.To == email);
        var rawToken = ExtractToken(emailMessage);

        Assert.DoesNotContain(rawToken, responseBody);
        Assert.Contains(
            "https://frontend.integration.example/accept-invitation",
            emailMessage.TextBody);
        Assert.DoesNotContain(
            "attacker-controlled.example",
            emailMessage.TextBody);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var persistedUser = await dbContext.Users
            .SingleAsync(existingUser => existingUser.Id == user.Id);
        var persistedToken = await dbContext.AccountTokens
            .SingleAsync(token => token.UserId == user.Id);
        var audit = await dbContext.AuditLogs
            .SingleAsync(log =>
                log.EntityId == user.Id.ToString() &&
                log.Action == "UserInvited");

        Assert.Null(persistedUser.PasswordHash);
        Assert.Null(persistedUser.InvitationAcceptedAt);
        Assert.NotEqual(rawToken, persistedToken.TokenHash);
        Assert.DoesNotContain(rawToken, persistedToken.TokenHash);
        Assert.DoesNotContain(rawToken, audit.OldValues ?? string.Empty);
        Assert.DoesNotContain(rawToken, audit.NewValues ?? string.Empty);
    }

    [Fact]
    public async Task InviteUser_ByEmployee_ReturnsForbidden()
    {
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/users/invitations",
            employeeToken,
            new InviteUserRequest(
                "Forbidden Invite",
                UniqueEmail("forbidden-invite"),
                UserRole.Employee,
                await GetDepartmentIdAsync()));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResendInvitation_ForPendingUser_RevokesPreviousToken()
    {
        var pending = await CreatePendingUserAsync(
            UserRole.Employee,
            UniqueEmail("resend"));
        var previousRawToken = await CreateAccountTokenAsync(
            pending.Id,
            AccountTokenType.Invitation);
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/users/{pending.Id}/invitations/resend",
            adminToken);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var emailMessage = Assert.Single(
            _emailSender.Messages,
            message => message.To == pending.Email);
        var newRawToken = ExtractToken(emailMessage);
        Assert.NotEqual(previousRawToken, newRawToken);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var tokens = await dbContext.AccountTokens
            .Where(token => token.UserId == pending.Id)
            .OrderBy(token => token.CreatedAt)
            .ToListAsync();

        Assert.Equal(2, tokens.Count);
        Assert.NotNull(tokens[0].RevokedAt);
        Assert.True(tokens[1].CanBeUsed(DateTime.UtcNow));
        Assert.Contains(
            await dbContext.AuditLogs.ToListAsync(),
            log =>
                log.EntityId == pending.Id.ToString() &&
                log.Action == "UserInvitationResent");
    }

    [Fact]
    public async Task ResendInvitation_ParallelRequests_LeavesSingleActiveToken()
    {
        var pending = await CreatePendingUserAsync(
            UserRole.Employee,
            UniqueEmail("parallel-resend"));
        await CreateAccountTokenAsync(
            pending.Id,
            AccountTokenType.Invitation);
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        async Task<HttpResponseMessage> ResendAsync()
        {
            using var request = CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/users/{pending.Id}/invitations/resend",
                adminToken);
            return await _client.SendAsync(request);
        }

        var responses = await Task.WhenAll(
            ResendAsync(),
            ResendAsync());

        Assert.Contains(
            responses,
            response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.All(
            responses,
            response => Assert.Contains(
                response.StatusCode,
                new[]
                {
                    HttpStatusCode.NoContent,
                    HttpStatusCode.Conflict
                }));

        foreach (var response in responses)
        {
            response.Dispose();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var tokens = await dbContext.AccountTokens
            .Where(token =>
                token.UserId == pending.Id &&
                token.Type == AccountTokenType.Invitation)
            .ToListAsync();

        Assert.Single(
            tokens,
            token => token.CanBeUsed(DateTime.UtcNow));
    }

    [Fact]
    public async Task AcceptInvitation_ParallelRequests_OnlyOneSucceeds()
    {
        var pending = await CreatePendingUserAsync(
            UserRole.Employee,
            UniqueEmail("parallel-accept"));
        var rawToken = await CreateAccountTokenAsync(
            pending.Id,
            AccountTokenType.Invitation);

        var request = new AcceptInvitationRequest(
            rawToken,
            "AcceptedPassword123!");

        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync(
                "/api/auth/invitations/accept",
                request),
            _client.PostAsJsonAsync(
                "/api/auth/invitations/accept",
                request));

        Assert.Equal(
            1,
            responses.Count(response =>
                response.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(
            1,
            responses.Count(response =>
                response.StatusCode is
                    HttpStatusCode.BadRequest or
                    HttpStatusCode.Conflict));

        foreach (var response in responses)
        {
            response.Dispose();
        }

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users
            .SingleAsync(existingUser => existingUser.Id == pending.Id);
        var token = await dbContext.AccountTokens
            .SingleAsync(existingToken =>
                existingToken.UserId == pending.Id);

        Assert.True(user.IsOperational);
        Assert.NotNull(token.UsedAt);
        Assert.Equal(
            1,
            await dbContext.AuditLogs.CountAsync(log =>
                log.EntityId == pending.Id.ToString() &&
                log.Action == "UserInvitationAccepted"));
    }

    [Fact]
    public async Task AcceptInvitation_WithExpiredToken_ReturnsBadRequest()
    {
        var pending = await CreatePendingUserAsync(
            UserRole.Employee,
            UniqueEmail("expired-accept"));
        var rawToken = await CreateAccountTokenAsync(
            pending.Id,
            AccountTokenType.Invitation,
            expiresAt: DateTime.UtcNow.AddHours(-1));

        using var response = await _client.PostAsJsonAsync(
            "/api/auth/invitations/accept",
            new AcceptInvitationRequest(
                rawToken,
                "AcceptedPassword123!"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ForgotPassword_ForAllAccountStates_ReturnsIdenticalAcceptedResponse()
    {
        var active = await CreateOperationalUserAsync(
            UserRole.Employee,
            UniqueEmail("forgot-active"),
            "OldPassword123!");
        var pending = await CreatePendingUserAsync(
            UserRole.Employee,
            UniqueEmail("forgot-pending"));
        var inactive = await CreateOperationalUserAsync(
            UserRole.Employee,
            UniqueEmail("forgot-inactive"),
            "OldPassword123!",
            isActive: false);

        var emails = new[]
        {
            active.Email,
            UniqueEmail("forgot-unknown"),
            pending.Email,
            inactive.Email
        };

        var messages = new List<string>();

        foreach (var email in emails)
        {
            using var response = await _client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new ForgotPasswordRequest(email));

            Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
            var body = await response.Content
                .ReadFromJsonAsync<ForgotPasswordResponse>(JsonOptions);
            messages.Add(body!.Message);
        }

        Assert.Single(messages.Distinct());
        Assert.Single(
            _emailSender.Messages,
            message => message.To == active.Email);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var resetTokens = await dbContext.AccountTokens
            .Where(token =>
                token.Type == AccountTokenType.PasswordReset &&
                new[] { active.Id, pending.Id, inactive.Id }
                    .Contains(token.UserId))
            .ToListAsync();

        var resetToken = Assert.Single(resetTokens);
        Assert.Equal(active.Id, resetToken.UserId);
        var rawToken = ExtractToken(
            Assert.Single(
                _emailSender.Messages,
                message => message.To == active.Email));
        Assert.DoesNotContain(rawToken, resetToken.TokenHash);
    }

    [Fact]
    public async Task ResetPassword_ParallelRequests_OnlyOneSucceedsAndOldJwtIsInvalid()
    {
        var email = UniqueEmail("parallel-reset");
        var user = await CreateOperationalUserAsync(
            UserRole.Employee,
            email,
            "OldPassword123!");
        var oldJwt = await LoginAsync(email, "OldPassword123!");
        var rawToken = await CreateAccountTokenAsync(
            user.Id,
            AccountTokenType.PasswordReset);
        await CreateAccountTokenAsync(
            user.Id,
            AccountTokenType.PasswordReset);

        var request = new ResetPasswordRequest(
            rawToken,
            "ResetPassword123!");
        var responses = await Task.WhenAll(
            _client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request),
            _client.PostAsJsonAsync(
                "/api/auth/reset-password",
                request));

        Assert.Equal(
            1,
            responses.Count(response =>
                response.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(
            1,
            responses.Count(response =>
                response.StatusCode is
                    HttpStatusCode.BadRequest or
                    HttpStatusCode.Conflict));

        foreach (var response in responses)
        {
            response.Dispose();
        }

        using var meRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/auth/me",
            oldJwt);
        using var meResponse = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        await LoginAsync(email, "ResetPassword123!");

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var tokens = await dbContext.AccountTokens
            .Where(token =>
                token.UserId == user.Id &&
                token.Type == AccountTokenType.PasswordReset)
            .ToListAsync();

        Assert.Single(tokens, token => token.UsedAt.HasValue);
        Assert.Single(tokens, token => token.RevokedAt.HasValue);
        Assert.Equal(
            1,
            await dbContext.AuditLogs.CountAsync(log =>
                log.EntityId == user.Id.ToString() &&
                log.Action == "UserPasswordReset"));
    }

    [Fact]
    public async Task ChangePassword_WithValidCurrentPassword_InvalidatesCurrentJwt()
    {
        var email = UniqueEmail("change-password");
        var user = await CreateOperationalUserAsync(
            UserRole.Employee,
            email,
            "OldPassword123!");
        var oldJwt = await LoginAsync(email, "OldPassword123!");

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/auth/change-password",
            oldJwt,
            new ChangePasswordRequest(
                "OldPassword123!",
                "NewPassword123!"));
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var meRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/auth/me",
            oldJwt);
        using var meResponse = await _client.SendAsync(meRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);

        await LoginAsync(email, "NewPassword123!");

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        Assert.Contains(
            await dbContext.AuditLogs.ToListAsync(),
            log =>
                log.EntityId == user.Id.ToString() &&
                log.Action == "UserPasswordChanged" &&
                !(log.NewValues ?? string.Empty).Contains(
                    "NewPassword123!",
                    StringComparison.Ordinal));
    }

    [Fact]
    public async Task Profile_ReturnsCompleteDbBackedContract()
    {
        var token = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/auth/me",
            token);
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<
            CurrentUserDto>(JsonOptions);

        Assert.NotNull(profile);
        Assert.NotEqual(Guid.Empty, profile.Id);
        Assert.False(string.IsNullOrWhiteSpace(profile.FullName));
        Assert.False(string.IsNullOrWhiteSpace(profile.Email));
        Assert.False(string.IsNullOrWhiteSpace(profile.Role));
        Assert.NotEqual(Guid.Empty, profile.DepartmentId);
        Assert.False(string.IsNullOrWhiteSpace(profile.DepartmentName));
        Assert.True(profile.IsActive);
        Assert.Equal("Active", profile.AccountStatus);
    }

    [Fact]
    public async Task AssignTicket_WithPendingTechnician_ReturnsBadRequest()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var pendingTechnician = await CreatePendingUserAsync(
            UserRole.Technician,
            UniqueEmail("pending-technician"));
        var ticketId = await CreateOpenTicketAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{ticketId}/assignment",
            adminToken,
            new AssignTicketRequest
            {
                TechnicianId = pendingTechnician.Id
            });
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangeRole_WhenOnlyOtherAdminIsPending_ReturnsConflict()
    {
        await CreatePendingUserAsync(
            UserRole.Admin,
            UniqueEmail("pending-admin"));
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var adminId = await GetUserIdByEmailAsync(
            CustomWebApplicationFactory.AdminEmail);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/users/{adminId}/role",
            adminToken,
            new ChangeUserRoleRequest
            {
                Role = UserRole.Employee
            });
        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task InviteUser_WhenEmailDeliveryFails_ReturnsServiceUnavailableButKeepsState()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var email = UniqueEmail("delivery-failure");
        _emailSender.FailNextDelivery();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/users/invitations",
            adminToken,
            new InviteUserRequest(
                "Delivery Failure",
                email,
                UserRole.Employee,
                await GetDepartmentIdAsync()));
        using var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.ServiceUnavailable,
            response.StatusCode);

        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var user = await dbContext.Users
            .SingleAsync(existingUser => existingUser.Email == email);
        Assert.Contains(
            await dbContext.AccountTokens.ToListAsync(),
            token => token.UserId == user.Id);
    }

    [Fact]
    public async Task ForgotPassword_WhenLimitExceeded_ReturnsProblemDetails429()
    {
        await using var limitedFactory =
            new LimitedRateWebApplicationFactory();
        using var client = limitedFactory.CreateClient();

        using var firstResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(UniqueEmail("rate-first")));
        using var secondResponse = await client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            new ForgotPasswordRequest(UniqueEmail("rate-second")));

        Assert.Equal(HttpStatusCode.Accepted, firstResponse.StatusCode);
        Assert.Equal(
            HttpStatusCode.TooManyRequests,
            secondResponse.StatusCode);

        var problem = await secondResponse.Content
            .ReadFromJsonAsync<JsonElement>();
        Assert.Equal(
            StatusCodes.Status429TooManyRequests,
            problem.GetProperty("status").GetInt32());
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        using var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = email,
                Password = password
            });
        response.EnsureSuccessStatusCode();

        var login = await response.Content
            .ReadFromJsonAsync<LoginResponse>(JsonOptions);
        return login!.AccessToken;
    }

    private async Task<Guid> GetDepartmentIdAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        return await dbContext.Departments
            .Where(department => department.IsActive)
            .Select(department => department.Id)
            .FirstAsync();
    }

    private async Task<Guid> GetUserIdByEmailAsync(string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        return await dbContext.Users
            .Where(user => user.Email == email)
            .Select(user => user.Id)
            .SingleAsync();
    }

    private async Task<User> CreateOperationalUserAsync(
        UserRole role,
        string email,
        string password,
        bool isActive = true)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services
            .GetRequiredService<ApplicationDbContext>();
        var passwordHashService = services
            .GetRequiredService<IPasswordHashService>();
        var user = new User(
            "Lifecycle User",
            email,
            passwordHashService.HashPassword(password),
            role,
            await dbContext.Departments
                .Select(department => department.Id)
                .FirstAsync());

        if (!isActive)
        {
            user.Deactivate();
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<User> CreatePendingUserAsync(
        UserRole role,
        string email)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var user = User.CreateInvited(
            "Pending Lifecycle User",
            email,
            role,
            await dbContext.Departments
                .Select(department => department.Id)
                .FirstAsync());
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    private async Task<string> CreateAccountTokenAsync(
        Guid userId,
        AccountTokenType type,
        DateTime? expiresAt = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var dbContext = services
            .GetRequiredService<ApplicationDbContext>();
        var generator = services
            .GetRequiredService<IAccountTokenGenerator>();
        var generated = generator.Generate();
        var token = new AccountToken(
            userId,
            generated.TokenHash,
            type,
            DateTime.UtcNow.AddHours(1));

        if (expiresAt.HasValue)
        {
            SetProperty(token, nameof(AccountToken.ExpiresAt), expiresAt.Value);
        }

        dbContext.AccountTokens.Add(token);
        await dbContext.SaveChangesAsync();
        return generated.RawToken;
    }

    private async Task<Guid> CreateOpenTicketAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var adminId = await dbContext.Users
            .Where(user =>
                user.Email == CustomWebApplicationFactory.AdminEmail)
            .Select(user => user.Id)
            .SingleAsync();
        var departmentId = await dbContext.Departments
            .Select(department => department.Id)
            .FirstAsync();
        var asset = new Asset(
            "Pending Technician Test Asset",
            $"PTS-{Guid.NewGuid():N}",
            AssetType.Computer,
            departmentId,
            "Integration Test");
        dbContext.Assets.Add(asset);
        var ticket = new Ticket(
            asset.Id,
            adminId,
            "Pending technician security test",
            "Pending technician must not receive this ticket.",
            TicketPriority.Medium);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync();
        return ticket.Id;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, requestUri);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }

    private static string ExtractToken(EmailMessage message)
    {
        const string marker = "?token=";
        var markerIndex = message.TextBody.IndexOf(
            marker,
            StringComparison.Ordinal);

        Assert.True(markerIndex >= 0);

        var tokenStart = markerIndex + marker.Length;
        var tokenEnd = message.TextBody.IndexOfAny(
            ['\r', '\n', ' ', '\t'],
            tokenStart);

        var encodedToken = tokenEnd < 0
            ? message.TextBody[tokenStart..]
            : message.TextBody[tokenStart..tokenEnd];

        return Uri.UnescapeDataString(encodedToken);
    }

    private static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, JsonOptions)!;
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}-{Guid.NewGuid():N}@example.com";

    private static void SetProperty<T>(
        object instance,
        string propertyName,
        T value)
    {
        var property = instance.GetType().GetProperty(
            propertyName,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic);

        property!.SetValue(instance, value);
    }

    private sealed class LimitedRateWebApplicationFactory
        : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureAppConfiguration(
                (_, configuration) =>
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["RateLimiting:Account:ForgotPassword:PermitLimit"] = "1",
                            ["RateLimiting:Account:ForgotPassword:WindowSeconds"] = "60"
                        }));
        }
    }
}
