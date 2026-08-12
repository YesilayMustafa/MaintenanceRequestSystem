using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Categories.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Categories;

public sealed class TicketCategoryManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public TicketCategoryManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetCategories_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/categories");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Employee_CanReadActiveCategories_ButCannotManageThem()
    {
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var getRequest = Authorized(
            HttpMethod.Get,
            "/api/categories",
            employeeToken);
        var getResponse = await _client.SendAsync(getRequest);

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var categories = await getResponse.Content
            .ReadFromJsonAsync<List<TicketCategoryDto>>();
        Assert.NotNull(categories);
        Assert.NotEmpty(categories);
        Assert.All(categories, category => Assert.True(category.IsActive));

        using var inactiveRequest = Authorized(
            HttpMethod.Get,
            "/api/categories?includeInactive=true",
            employeeToken);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.SendAsync(inactiveRequest)).StatusCode);

        using var createRequest = Authorized(
            HttpMethod.Post,
            "/api/categories",
            employeeToken,
            new CreateTicketCategoryRequest { Name = "Yetkisiz" });
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.SendAsync(createRequest)).StatusCode);
    }

    [Fact]
    public async Task Technician_CanReadActiveCategories_ButCannotMutateThem()
    {
        var adminToken = await AdminLoginAsync();
        var technician = await CreateTechnicianAsync(adminToken);
        var technicianToken = await LoginAsync(
            technician.Email,
            technician.Password);

        using var getRequest = Authorized(
            HttpMethod.Get,
            "/api/categories",
            technicianToken);
        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.SendAsync(getRequest)).StatusCode);

        using var updateRequest = Authorized(
            HttpMethod.Put,
            $"/api/categories/{TicketCategory.OtherId}",
            technicianToken,
            new UpdateTicketCategoryRequest { Name = "Yetkisiz" });
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.SendAsync(updateRequest)).StatusCode);

        using var statusRequest = Authorized(
            HttpMethod.Patch,
            $"/api/categories/{TicketCategory.OtherId}/status",
            technicianToken,
            new ChangeTicketCategoryStatusRequest { IsActive = false });
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.SendAsync(statusRequest)).StatusCode);
    }

    [Fact]
    public async Task Admin_CanCreateUpdateDeactivateActivateAndAuditCategory()
    {
        var adminToken = await AdminLoginAsync();
        var category = await CreateCategoryAsync(
            adminToken,
            $"Kategori {Guid.NewGuid():N}");

        using var updateRequest = Authorized(
            HttpMethod.Put,
            $"/api/categories/{category.Id}",
            adminToken,
            new UpdateTicketCategoryRequest
            {
                Name = $"Güncel {Guid.NewGuid():N}",
                Description = "Güncel açıklama"
            });
        var updateResponse = await _client.SendAsync(updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content
            .ReadFromJsonAsync<TicketCategoryDto>();
        Assert.NotNull(updated);
        Assert.Equal("Güncel açıklama", updated.Description);

        await ChangeStatusAsync(adminToken, category.Id, false);

        var activeCategories = await GetCategoriesAsync(adminToken, false);
        Assert.DoesNotContain(activeCategories, item => item.Id == category.Id);

        var allCategories = await GetCategoriesAsync(adminToken, true);
        Assert.Contains(
            allCategories,
            item => item.Id == category.Id && !item.IsActive);

        await ChangeStatusAsync(adminToken, category.Id, true);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var actions = context.AuditLogs
            .Where(log => log.EntityId == category.Id.ToString())
            .Select(log => log.Action)
            .ToList();

        Assert.Contains("TicketCategoryCreated", actions);
        Assert.Contains("TicketCategoryUpdated", actions);
        Assert.Contains("TicketCategoryDeactivated", actions);
        Assert.Contains("TicketCategoryActivated", actions);
    }

    [Fact]
    public async Task CreateCategory_WithNormalizedDuplicate_ReturnsConflict()
    {
        var adminToken = await AdminLoginAsync();
        var name = $"Yazılım Test {Guid.NewGuid():N}";
        await CreateCategoryAsync(adminToken, name);

        using var request = Authorized(
            HttpMethod.Post,
            "/api/categories",
            adminToken,
            new CreateTicketCategoryRequest
            {
                Name = $"  {name.ToUpper(new System.Globalization.CultureInfo("tr-TR"))}  "
            });

        Assert.Equal(
            HttpStatusCode.Conflict,
            (await _client.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task DeactivateLastActiveCategory_ReturnsConflict()
    {
        var adminToken = await AdminLoginAsync();
        var categories = await GetCategoriesAsync(adminToken, true);

        try
        {
            foreach (var category in categories)
            {
                if (!category.IsActive)
                {
                    await ChangeStatusAsync(adminToken, category.Id, true);
                }
            }

            var active = await GetCategoriesAsync(adminToken, false);

            foreach (var category in active.Skip(1))
            {
                await ChangeStatusAsync(adminToken, category.Id, false);
            }

            using var lastRequest = Authorized(
                HttpMethod.Patch,
                $"/api/categories/{active[0].Id}/status",
                adminToken,
                new ChangeTicketCategoryStatusRequest { IsActive = false });

            Assert.Equal(
                HttpStatusCode.Conflict,
                (await _client.SendAsync(lastRequest)).StatusCode);
        }
        finally
        {
            foreach (var category in categories)
            {
                await ChangeStatusAsync(adminToken, category.Id, true);
            }
        }
    }

    [Fact]
    public async Task CreateTicket_ValidatesCategoryAndReturnsCategoryFields()
    {
        var adminToken = await AdminLoginAsync();
        var employeeToken = await EmployeeLoginAsync();
        var category = await CreateCategoryAsync(
            adminToken,
            $"Ticket {Guid.NewGuid():N}");
        var asset = await CreateAssetAsync(adminToken);

        var ticket = await CreateTicketAsync(
            employeeToken,
            asset.Id,
            category.Id);

        Assert.Equal(category.Id, ticket.CategoryId);
        Assert.Equal(category.Name, ticket.CategoryName);
        Assert.Matches("^REQ-[0-9]{4}-[0-9]{6}$", ticket.TicketNumber);

        using var missingRequest = Authorized(
            HttpMethod.Post,
            "/api/tickets",
            employeeToken,
            TicketRequest(asset.Id, Guid.Empty));
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await _client.SendAsync(missingRequest)).StatusCode);

        using var unknownRequest = Authorized(
            HttpMethod.Post,
            "/api/tickets",
            employeeToken,
            TicketRequest(asset.Id, Guid.NewGuid()));
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.SendAsync(unknownRequest)).StatusCode);

        await ChangeStatusAsync(adminToken, category.Id, false);

        using var inactiveRequest = Authorized(
            HttpMethod.Post,
            "/api/tickets",
            employeeToken,
            TicketRequest(asset.Id, category.Id));
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await _client.SendAsync(inactiveRequest)).StatusCode);
    }

    [Fact]
    public async Task InactiveCategory_RemainsVisibleOnExistingTicket()
    {
        var adminToken = await AdminLoginAsync();
        var employeeToken = await EmployeeLoginAsync();
        var category = await CreateCategoryAsync(
            adminToken,
            $"Tarihsel {Guid.NewGuid():N}");
        var asset = await CreateAssetAsync(adminToken);
        var ticket = await CreateTicketAsync(
            employeeToken,
            asset.Id,
            category.Id);

        await ChangeStatusAsync(adminToken, category.Id, false);

        using var request = Authorized(
            HttpMethod.Get,
            $"/api/tickets/{ticket.Id}",
            employeeToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(detail);
        Assert.Equal(category.Id, detail.CategoryId);
        Assert.Equal(category.Name, detail.CategoryName);
    }

    [Fact]
    public async Task ChangeTicketCategory_IsAdminOnlyAndCreatesHistoryAndAudit()
    {
        var adminToken = await AdminLoginAsync();
        var employeeToken = await EmployeeLoginAsync();
        var oldCategory = await CreateCategoryAsync(
            adminToken,
            $"Eski {Guid.NewGuid():N}");
        var newCategory = await CreateCategoryAsync(
            adminToken,
            $"Yeni {Guid.NewGuid():N}");
        var asset = await CreateAssetAsync(adminToken);
        var ticket = await CreateTicketAsync(
            employeeToken,
            asset.Id,
            oldCategory.Id);
        var body = new ChangeTicketCategoryRequest
        {
            CategoryId = newCategory.Id
        };

        using var forbiddenRequest = Authorized(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/category",
            employeeToken,
            body);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.SendAsync(forbiddenRequest)).StatusCode);

        var technician = await CreateTechnicianAsync(adminToken);
        var technicianToken = await LoginAsync(
            technician.Email,
            technician.Password);
        using var technicianRequest = Authorized(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/category",
            technicianToken,
            body);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await _client.SendAsync(technicianRequest)).StatusCode);

        using var unknownRequest = Authorized(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/category",
            adminToken,
            new ChangeTicketCategoryRequest { CategoryId = Guid.NewGuid() });
        Assert.Equal(
            HttpStatusCode.NotFound,
            (await _client.SendAsync(unknownRequest)).StatusCode);

        var inactiveCategory = await CreateCategoryAsync(
            adminToken,
            $"Pasif {Guid.NewGuid():N}");
        await ChangeStatusAsync(adminToken, inactiveCategory.Id, false);
        using var inactiveRequest = Authorized(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/category",
            adminToken,
            new ChangeTicketCategoryRequest
            {
                CategoryId = inactiveCategory.Id
            });
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await _client.SendAsync(inactiveRequest)).StatusCode);

        using var changeRequest = Authorized(
            HttpMethod.Patch,
            $"/api/tickets/{ticket.Id}/category",
            adminToken,
            body);
        var changeResponse = await _client.SendAsync(changeRequest);
        changeResponse.EnsureSuccessStatusCode();
        var changed = await changeResponse.Content
            .ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(changed);
        Assert.Equal(newCategory.Id, changed.CategoryId);
        Assert.Equal(newCategory.Name, changed.CategoryName);

        using var historyRequest = Authorized(
            HttpMethod.Get,
            $"/api/tickets/{ticket.Id}/history",
            adminToken);
        var histories = await (await _client.SendAsync(historyRequest))
            .Content.ReadFromJsonAsync<List<TicketHistoryDto>>();
        Assert.NotNull(histories);
        Assert.Contains(
            histories,
            history => history.Description.Contains("kategorisi"));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        Assert.Contains(
            context.AuditLogs,
            log => log.Action == "TicketCategoryChanged" &&
                log.EntityId == ticket.Id.ToString());
    }

    private Task<string> AdminLoginAsync() => LoginAsync(
        CustomWebApplicationFactory.AdminEmail,
        CustomWebApplicationFactory.AdminPassword);

    private Task<string> EmployeeLoginAsync() => LoginAsync(
        CustomWebApplicationFactory.EmployeeEmail,
        CustomWebApplicationFactory.EmployeePassword);

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login.AccessToken;
    }

    private async Task<TicketCategoryDto> CreateCategoryAsync(
        string token,
        string name)
    {
        using var request = Authorized(
            HttpMethod.Post,
            "/api/categories",
            token,
            new CreateTicketCategoryRequest { Name = name });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var category = await response.Content
            .ReadFromJsonAsync<TicketCategoryDto>();
        Assert.NotNull(category);
        return category;
    }

    private async Task<List<TicketCategoryDto>> GetCategoriesAsync(
        string token,
        bool includeInactive)
    {
        using var request = Authorized(
            HttpMethod.Get,
            $"/api/categories?includeInactive={includeInactive.ToString().ToLowerInvariant()}",
            token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var categories = await response.Content
            .ReadFromJsonAsync<List<TicketCategoryDto>>();
        Assert.NotNull(categories);
        return categories;
    }

    private async Task ChangeStatusAsync(
        string token,
        Guid id,
        bool isActive)
    {
        using var request = Authorized(
            HttpMethod.Patch,
            $"/api/categories/{id}/status",
            token,
            new ChangeTicketCategoryStatusRequest { IsActive = isActive });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<AssetDto> CreateAssetAsync(string adminToken)
    {
        using var departmentsRequest = Authorized(
            HttpMethod.Get,
            "/api/departments",
            adminToken);
        var departmentsResponse = await _client.SendAsync(departmentsRequest);
        departmentsResponse.EnsureSuccessStatusCode();
        var departments = await departmentsResponse.Content
            .ReadFromJsonAsync<List<DepartmentDto>>();
        Assert.NotNull(departments);
        var departmentId = departments.First(item => item.IsActive).Id;

        using var request = Authorized(
            HttpMethod.Post,
            "/api/assets",
            adminToken,
            new CreateAssetRequest
            {
                Name = "Kategori Test Cihazı",
                SerialNumber = $"CATEGORY-{Guid.NewGuid():N}",
                Type = AssetType.Computer,
                DepartmentId = departmentId
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var asset = await response.Content.ReadFromJsonAsync<AssetDto>();
        Assert.NotNull(asset);
        return asset;
    }

    private async Task<TicketDto> CreateTicketAsync(
        string employeeToken,
        Guid assetId,
        Guid categoryId)
    {
        using var request = Authorized(
            HttpMethod.Post,
            "/api/tickets",
            employeeToken,
            TicketRequest(assetId, categoryId));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        return ticket;
    }

    private static CreateTicketRequest TicketRequest(
        Guid assetId,
        Guid categoryId)
    {
        return new CreateTicketRequest
        {
            AssetId = assetId,
            CategoryId = categoryId,
            Title = $"Kategori test talebi {Guid.NewGuid():N}",
            Description = "Kategori integration testi açıklaması.",
            Priority = TicketPriority.Medium
        };
    }

    private async Task<(string Email, string Password)> CreateTechnicianAsync(
        string adminToken)
    {
        using var departmentsRequest = Authorized(
            HttpMethod.Get,
            "/api/departments",
            adminToken);
        var departments = await (await _client.SendAsync(departmentsRequest))
            .Content.ReadFromJsonAsync<List<DepartmentDto>>();
        Assert.NotNull(departments);

        var email = $"category-tech-{Guid.NewGuid():N}@example.com";
        const string password = "TechnicianTest123!";
        using var request = Authorized(
            HttpMethod.Post,
            "/api/users",
            adminToken,
            new CreateUserRequest
            {
                FullName = "Kategori Teknisyeni",
                Email = email,
                Password = password,
                Role = UserRole.Technician,
                DepartmentId = departments.First(item => item.IsActive).Id
            });
        (await _client.SendAsync(request)).EnsureSuccessStatusCode();
        return (email, password);
    }

    private static HttpRequestMessage Authorized(
        HttpMethod method,
        string uri,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        return request;
    }
}
