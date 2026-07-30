using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed class TicketManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TicketManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateTicket_WithoutToken_ReturnsUnauthorized()
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/tickets",
                new CreateTicketRequest());

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_WithEmployeeToken_ReturnsCreatedOpenTicket()
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

        var requestBody =
            CreateTicketRequestFor(asset.Id);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(ticket);
        Assert.Equal("Open", ticket.Status);
        Assert.Equal("High", ticket.Priority);
        Assert.Equal(asset.Id, ticket.AssetId);

        Assert.Equal(
            "Test Çalışanı",
            ticket.CreatedByFullName);

        Assert.Null(ticket.AssignedTechnicianId);
    }

    [Fact]
    public async Task GetTicket_WithCreatorToken_ReturnsOk()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(request);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(ticket);

        Assert.Equal(
            setup.Ticket.Id,
            ticket.Id);

        Assert.Equal(
            "Test Çalışanı",
            ticket.CreatedByFullName);
    }

    [Fact]
    public async Task GetTicket_WithAnotherEmployeeToken_ReturnsForbidden()
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
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                secondEmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var responseText =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "Başka bir kullanıcıya ait talebi",
            responseText);
    }

    [Fact]
    public async Task GetTicket_WithAdminToken_ReturnsOk()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_WithMissingAsset_ReturnsNotFound()
    {
        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var requestBody =
            CreateTicketRequestFor(
                Guid.NewGuid());

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateTicket_WithInactiveAsset_ReturnsBadRequest()
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

        using var statusRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/assets/{asset.Id}/status",
                adminToken,
                new ChangeAssetStatusRequest
                {
                    IsActive = false
                });

        var statusResponse =
            await _client.SendAsync(statusRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            statusResponse.StatusCode);

        using var createRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                CreateTicketRequestFor(asset.Id));

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            createResponse.StatusCode);
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

        return new CreatedEmployee(
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

    private sealed record TicketSetup(
        string AdminToken,
        string EmployeeToken,
        TicketDto Ticket);

    private sealed record CreatedEmployee(
        string Email,
        string Password);
}