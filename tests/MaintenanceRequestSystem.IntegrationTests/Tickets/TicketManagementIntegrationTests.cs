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
using MaintenanceRequestSystem.Application.Common.Models;

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
    public async Task GetTickets_WithPagination_ReturnsCorrectPages()
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
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Pagination Talebi 1",
            TicketPriority.Low);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Pagination Talebi 2",
            TicketPriority.Medium);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Pagination Talebi 3",
            TicketPriority.High);

        var firstPage =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=1&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(1, firstPage.PageNumber);
        Assert.Equal(2, firstPage.PageSize);

        var secondPage =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=2&pageSize=2");

        Assert.Single(secondPage.Items);
        Assert.Equal(2, secondPage.PageNumber);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);
    }
    [Fact]
    public async Task GetTickets_SortedByTitleAscending_ReturnsOrderedItems()
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
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "C Talebi",
            TicketPriority.Low);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "A Talebi",
            TicketPriority.Low);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "B Talebi",
            TicketPriority.Low);

        var result =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=1&pageSize=20" +
                "&sortBy=title&sortDescending=false");

        var titles =
            result.Items
                .Select(ticket => ticket.Title)
                .ToList();

        Assert.Equal(
            new[]
            {
            "A Talebi",
            "B Talebi",
            "C Talebi"
            },
            titles);
    }

    [Fact]
    public async Task GetTickets_WithInvalidPageSize_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/tickets?pageNumber=1&pageSize=101",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithFilters_ReturnsMatchingTickets()
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
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var highTicket =
            await CreateTicketAsync(
                employeeToken,
                asset.Id,
                "High filtre talebi",
                TicketPriority.High);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Critical filtre talebi",
            TicketPriority.Critical);

        var result =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&status=1&priority=3" +
                "&pageNumber=1&pageSize=20");

        var filteredTicket =
            Assert.Single(result.Items);

        Assert.Equal(
            highTicket.Id,
            filteredTicket.Id);

        Assert.Equal(
            "Open",
            filteredTicket.Status);

        Assert.Equal(
            "High",
            filteredTicket.Priority);
    }

    [Fact]
    public async Task GetTickets_WithEmployeeToken_ReturnsOnlyOwnTickets()
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
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var ownTicket =
            await CreateTicketAsync(
                employeeToken,
                asset.Id,
                "Çalışanın kendi talebi",
                TicketPriority.High);

        var secondEmployee =
            await CreateEmployeeAsync(adminToken);

        var secondEmployeeToken =
            await LoginAsync(
                secondEmployee.Email,
                secondEmployee.Password);

        await CreateTicketAsync(
            secondEmployeeToken,
            asset.Id,
            "Başka çalışanın talebi",
            TicketPriority.Medium);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=1&pageSize=20",
                employeeToken);

        var response =
            await _client.SendAsync(request);

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<TicketDto>>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(result);

        var visibleTicket =
            Assert.Single(result.Items);

        Assert.Equal(
            ownTicket.Id,
            visibleTicket.Id);
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