using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace MaintenanceRequestSystem.IntegrationTests.TicketComments;

public sealed class TicketCommentIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TicketCommentIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateComment_WithoutToken_ReturnsUnauthorized()
    {
        var response =
            await _client.PostAsJsonAsync(
                $"/api/tickets/{Guid.NewGuid()}/comments",
                new CreateTicketCommentRequest
                {
                    Content = "Test yorumu"
                });

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }



    [Fact]
    public async Task CreateComment_WithTicketOwnerToken_ReturnsCreated()
    {
        var setup =
            await CreateTicketSetupAsync();

        var comment =
            await CreateCommentAsync(
                setup.EmployeeToken,
                setup.Ticket.Id,
                "Çalışan tarafından eklenen yorum.");

        Assert.Equal(
            setup.Ticket.Id,
            comment.TicketId);

        Assert.Equal(
            "Test Çalışanı",
            comment.UserFullName);

        Assert.Equal(
            "Employee",
            comment.UserRole);
    }

    [Fact]
    public async Task GetComments_WithTicketOwnerToken_ReturnsCommentsInOrder()
    {
        var setup =
            await CreateTicketSetupAsync();

        var firstComment =
            await CreateCommentAsync(
                setup.EmployeeToken,
                setup.Ticket.Id,
                "Birinci yorum.");

        await Task.Delay(5);

        var secondComment =
            await CreateCommentAsync(
                setup.AdminToken,
                setup.Ticket.Id,
                "İkinci yorum.");

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(request);

        var comments =
            await response.Content
                .ReadFromJsonAsync<List<TicketCommentDto>>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(comments);
        Assert.Equal(2, comments.Count);

        Assert.Equal(
            firstComment.Id,
            comments[0].Id);

        Assert.Equal(
            secondComment.Id,
            comments[1].Id);
    }

    [Fact]
    public async Task CreateComment_WithAnotherEmployeeToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var secondEmployee =
            await CreateEmployeeAsync(
                setup.AdminToken);

        var secondEmployeeToken =
            await LoginAsync(
                secondEmployee.Email,
                secondEmployee.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                secondEmployeeToken,
                new CreateTicketCommentRequest
                {
                    Content =
                        "Başka çalışan tarafından eklenmeye çalışılan yorum."
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithUnsupportedRoleToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var accessToken =
            CreateTokenWithRole("999");

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                accessToken);
        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithAdminToken_ReturnsAdminComment()
    {
        var setup =
            await CreateTicketSetupAsync();

        var comment =
            await CreateCommentAsync(
                setup.AdminToken,
                setup.Ticket.Id,
                "Yönetici tarafından eklenen yorum.");

        Assert.Equal(
            "Sistem Yöneticisi",
            comment.UserFullName);

        Assert.Equal(
            "Admin",
            comment.UserRole);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                setup.AdminToken);

        var response =
            await _client.SendAsync(request);

        var comments =
            await response.Content
                .ReadFromJsonAsync<List<TicketCommentDto>>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(comments);

        Assert.Contains(
            comments,
            item => item.Id == comment.Id);
    }

    [Fact]
    public async Task GetComments_WithAssignedTechnicianToken_ReturnsOk()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            technician.Id);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                technicianToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetComments_WithDifferentTechnicianToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var differentTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            assignedTechnician.Id);

        var technicianToken =
            await LoginAsync(
                differentTechnician.Email,
                differentTechnician.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                technicianToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithAssignedTechnicianToken_ReturnsCreated()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            technician.Id);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        var comment =
            await CreateCommentAsync(
                technicianToken,
                setup.Ticket.Id,
                "Atanmış teknik personel yorumu.");

        Assert.Equal(
            setup.Ticket.Id,
            comment.TicketId);

        Assert.Equal(
            "Technician",
            comment.UserRole);
    }

    [Fact]
    public async Task CreateComment_WithDifferentTechnicianToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var differentTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            assignedTechnician.Id);

        var technicianToken =
            await LoginAsync(
                differentTechnician.Email,
                differentTechnician.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/tickets/{setup.Ticket.Id}/comments",
                technicianToken,
                new CreateTicketCommentRequest
                {
                    Content =
                        "Başka teknik personelin yorumu."
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    private async Task<CommentSetup>
        CreateTicketSetupAsync()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var ticket =
            await CreateTicketAsync(
                employeeToken,
                asset.Id);

        return new CommentSetup(
            adminToken,
            employeeToken,
            ticket);
    }

    private async Task<TicketCommentDto>
        CreateCommentAsync(
            string accessToken,
            Guid ticketId,
            string content)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                $"/api/tickets/{ticketId}/comments",
                accessToken,
                new CreateTicketCommentRequest
                {
                    Content = content
                });

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var comment =
            await response.Content
                .ReadFromJsonAsync<TicketCommentDto>();

        Assert.NotNull(comment);

        return comment;
    }

    private async Task<TicketDto> CreateTicketAsync(
        string employeeToken,
        Guid assetId)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                new CreateTicketRequest
                {
                    AssetId = assetId,
                    Title = "Yorum Test Talebi",
                    Description =
                        "Yorum integration testleri için oluşturuldu.",
                    Priority = TicketPriority.Medium
                });

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        return ticket;
    }

    private async Task<AssetDto> CreateAssetAsync(
        string adminToken,
        Guid departmentId)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                new CreateAssetRequest
                {
                    Name = "Yorum Test Cihazı",
                    SerialNumber =
                        $"COMMENT-{Guid.NewGuid():N}",
                    Type = AssetType.Computer,
                    DepartmentId = departmentId,
                    Location = "Test Odası"
                });

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var asset =
            await response.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.NotNull(asset);

        return asset;
    }

    private async Task<CreatedEmployee>
        CreateEmployeeAsync(
            string adminToken)
    {
        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var email =
            $"comment-user-{Guid.NewGuid():N}@example.com";

        const string password =
            "UserTest123!";

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                new CreateUserRequest
                {
                    FullName = "İkinci Yorum Çalışanı",
                    Email = email,
                    Password = password,
                    Role = UserRole.Employee,
                    DepartmentId = departmentId
                });

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return new CreatedEmployee(
            email,
            password);
    }

    private async Task<CreatedTechnician>
        CreateTechnicianAsync(
            string adminToken)
    {
        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var email =
            $"comment-technician-{Guid.NewGuid():N}@example.com";

        const string password =
            "TechnicianTest123!";

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                new CreateUserRequest
                {
                    FullName = "Yorum Test Teknik Personeli",
                    Email = email,
                    Password = password,
                    Role = UserRole.Technician,
                    DepartmentId = departmentId
                });

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var user =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.NotNull(user);

        return new CreatedTechnician(
            user.Id,
            email,
            password);
    }

    private async Task AssignTicketAsync(
        string adminToken,
        Guid ticketId,
        Guid technicianId)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{ticketId}/assignment",
                adminToken,
                new AssignTicketRequest
                {
                    TechnicianId = technicianId
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private async Task<Guid>
        GetActiveDepartmentIdAsync(
            string accessToken)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/departments",
                accessToken);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var departments =
            await response.Content
                .ReadFromJsonAsync<List<DepartmentDto>>();

        Assert.NotNull(departments);

        var activeDepartment =
            departments.FirstOrDefault(
                department => department.IsActive);

        Assert.NotNull(activeDepartment);

        return activeDepartment.Id;
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                new LoginRequest
                {
                    Email = email,
                    Password = password
                });

        response.EnsureSuccessStatusCode();

        var login =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(login);

        return login.AccessToken;
    }

    private static HttpRequestMessage
        CreateAuthorizedRequest(
            HttpMethod method,
            string requestUri,
            string accessToken,
            object? content = null)
    {
        var request =
            new HttpRequestMessage(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        if (content is not null)
        {
            request.Content =
                JsonContent.Create(content);
        }

        return request;
    }

    private string CreateTokenWithRole(
    string role)
    {
        var options =
            _factory.Services
                .GetRequiredService<IOptions<JwtOptions>>()
                .Value;

        var signingKey =
            new SymmetricSecurityKey(
                Convert.FromBase64String(
                    options.SigningKey));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var token =
            new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims:
                [
                    new Claim(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Name,
                    "Desteklenmeyen Rol Kullanıcısı"),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    "unsupported-role@example.com"),

                new Claim(
                    "role",
                    role),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
                ],
                notBefore: now,
                expires: now.AddMinutes(5),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private sealed record CommentSetup(
        string AdminToken,
        string EmployeeToken,
        TicketDto Ticket);

    private sealed record CreatedEmployee(
        string Email,
        string Password);

    private sealed record CreatedTechnician(
        Guid Id,
        string Email,
        string Password);
}
