using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
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

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    public TicketManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<TicketSetup> CreateResolvedTicketSetupAsync()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = technician.Id
                });

        var assignResponse =
            await _client.SendAsync(assignRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            assignResponse.StatusCode);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        var startResponse =
            await _client.SendAsync(startRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        using var resolveRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resolve",
                technicianToken,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Integration test çözümü."
                });

        var resolveResponse =
            await _client.SendAsync(resolveRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            resolveResponse.StatusCode);

        return setup;
    }

    private async Task<CreatedTechnician> CreateTechnicianAsync(
    string adminToken)
    {
        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var email =
            $"ticket-technician-{Guid.NewGuid():N}@example.com";

        const string password =
            "TechnicianTest123!";

        var requestBody =
            new CreateUserRequest
            {
                FullName = "Test Teknik Personeli",
                Email = email,
                Password = password,
                Role = UserRole.Technician,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                requestBody);

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

    private async Task<TicketSetup> CreateClosedTicketSetupAsync()
    {
        var setup =
            await CreateResolvedTicketSetupAsync();

        using var closeRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/close",
                setup.EmployeeToken);

        var closeResponse =
            await _client.SendAsync(closeRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            closeResponse.StatusCode);

        return setup;
    }

    private async Task<TicketDto> CreateTicketAsync(
    string accessToken,
    Guid assetId,
    string title,
    TicketPriority priority)
    {
        var requestBody =
            new CreateTicketRequest
            {
                AssetId = assetId,
                CategoryId = TicketCategory.OtherId,
                Title = title,
                Description =
                    $"{title} için integration test açıklaması.",
                Priority = priority
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                accessToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        return ticket;
    }

    private async Task<PagedResult<TicketDto>>
        GetPagedTicketsAsync(
            string accessToken,
            string requestUri)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                requestUri,
                accessToken);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<TicketDto>>();

        Assert.NotNull(result);

        return result;
    }

    private async Task<TicketSetup> CreateTicketSetupAsync()
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

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                CreateTicketRequestFor(asset.Id));

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        return new TicketSetup(
            adminToken,
            employeeToken,
            ticket);
    }

    private async Task<AssetDto> CreateAssetAsync(
        string adminToken,
        Guid departmentId)
    {
        var requestBody =
            new CreateAssetRequest
            {
                Name = "Ticket Test Cihazı",
                SerialNumber =
                    $"TICKET-ASSET-{Guid.NewGuid():N}",
                Type = AssetType.Computer,
                DepartmentId = departmentId,
                Location = "Test Odası"
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var asset =
            await response.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.NotNull(asset);

        return asset;
    }

    private async Task<CreatedEmployee> CreateEmployeeAsync(
        string adminToken)
    {
        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var email =
            $"ticket-user-{Guid.NewGuid():N}@example.com";

        const string password =
            "UserTest123!";

        var requestBody =
            new CreateUserRequest
            {
                FullName = "İkinci Test Çalışanı",
                Email = email,
                Password = password,
                Role = UserRole.Employee,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var user =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.NotNull(user);

        return new CreatedEmployee(
            user.Id,
            email,
            password);
    }

    private async Task<Guid> GetActiveDepartmentIdAsync(
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
        var loginRequest =
            new LoginRequest
            {
                Email = email,
                Password = password
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);

        return result.AccessToken;
    }

    private static CreateTicketRequest CreateTicketRequestFor(
        Guid assetId)
    {
        return new CreateTicketRequest
        {
            AssetId = assetId,
            CategoryId = TicketCategory.OtherId,
            Title = "Bilgisayar açılmıyor",
            Description =
                "Güç düğmesine basıldığında cihaz açılmıyor.",
            Priority = TicketPriority.High
        };
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
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

    private sealed record CreatedTechnician(
    Guid Id,
    string Email,
    string Password);

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

    private sealed record TicketSetup(
        string AdminToken,
        string EmployeeToken,
        TicketDto Ticket);

    private sealed record CreatedEmployee(
        Guid Id,
        string Email,
        string Password);
}
