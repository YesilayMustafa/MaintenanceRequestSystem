using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Dashboard;

public sealed class DashboardIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public DashboardIntegrationTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetDashboard_AppliesRoleScopesAndAdminWorkload()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);
        var departmentId = await GetDepartmentIdAsync(adminToken);

        var employee = await CreateUserAsync(
            adminToken,
            departmentId,
            UserRole.Employee,
            "Dashboard İkinci Çalışan");
        var technicianOne = await CreateUserAsync(
            adminToken,
            departmentId,
            UserRole.Technician,
            "Dashboard Teknisyen Bir");
        var technicianTwo = await CreateUserAsync(
            adminToken,
            departmentId,
            UserRole.Technician,
            "Dashboard Teknisyen İki");
        var inactiveTechnician = await CreateUserAsync(
            adminToken,
            departmentId,
            UserRole.Technician,
            "Dashboard Pasif Teknisyen");
        var pendingTechnician = await InviteTechnicianAsync(
            adminToken,
            departmentId);

        await SetUserStatusAsync(
            adminToken,
            inactiveTechnician.User.Id,
            false);

        var employeeTwoToken = await LoginAsync(
            employee.User.Email,
            employee.Password);
        var technicianOneToken = await LoginAsync(
            technicianOne.User.Email,
            technicianOne.Password);

        var asset = await CreateAssetAsync(adminToken, departmentId);

        var ownOpen = await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Çalışan bir açık kritik",
            TicketPriority.Critical);
        var ownAssigned = await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Çalışan bir atanmış",
            TicketPriority.High);
        var otherOpen = await CreateTicketAsync(
            employeeTwoToken,
            asset.Id,
            "Çalışan iki açık",
            TicketPriority.Medium);
        var otherAssigned = await CreateTicketAsync(
            employeeTwoToken,
            asset.Id,
            "Çalışan iki atanmış",
            TicketPriority.Low);

        await AssignAsync(adminToken, ownAssigned.Id, technicianOne.User.Id);
        await AssignAsync(adminToken, otherAssigned.Id, technicianTwo.User.Id);

        var employeeDashboard = await GetDashboardAsync(employeeToken);
        Assert.Equal(2, employeeDashboard.TotalCount);
        Assert.Equal(1, employeeDashboard.OpenCount);
        Assert.Equal(1, employeeDashboard.AssignedCount);
        Assert.Null(employeeDashboard.Admin);
        Assert.DoesNotContain(
            employeeDashboard.RecentTickets,
            ticket => ticket.Id == otherOpen.Id || ticket.Id == otherAssigned.Id);

        var technicianDashboard = await GetDashboardAsync(technicianOneToken);
        Assert.Equal(1, technicianDashboard.TotalCount);
        Assert.Equal(1, technicianDashboard.AssignedCount);
        Assert.Single(technicianDashboard.RecentTickets);
        Assert.Equal(ownAssigned.Id, technicianDashboard.RecentTickets[0].Id);
        Assert.Null(technicianDashboard.Admin);

        var adminDashboard = await GetDashboardAsync(adminToken);
        Assert.Equal(4, adminDashboard.TotalCount);
        Assert.Equal(4, adminDashboard.ActiveCount);
        Assert.NotNull(adminDashboard.Admin);
        Assert.Equal(2, adminDashboard.Admin.UnassignedOpenCount);

        var workloads = adminDashboard.Admin.TechnicianWorkload;
        Assert.Contains(workloads, item =>
            item.TechnicianId == technicianOne.User.Id &&
            item.ActiveTicketCount == 1);
        Assert.Contains(workloads, item =>
            item.TechnicianId == technicianTwo.User.Id &&
            item.ActiveTicketCount == 1);
        Assert.DoesNotContain(
            workloads,
            item => item.TechnicianId == inactiveTechnician.User.Id);
        Assert.DoesNotContain(
            workloads,
            item => item.TechnicianId == pendingTechnician.Id);
        Assert.Contains(
            adminDashboard.RecentTickets,
            ticket => ticket.Id == ownOpen.Id);
    }

    [Fact]
    public async Task GetDashboard_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/dashboard");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<DashboardDto> GetDashboardAsync(string token)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Get,
            "/api/dashboard",
            token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dashboard);
        return dashboard;
    }

    private async Task<CreatedUser> CreateUserAsync(
        string adminToken,
        Guid departmentId,
        UserRole role,
        string fullName)
    {
        var email = $"dashboard-{Guid.NewGuid():N}@example.com";
        const string password = "DashboardTest123!";
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            "/api/users",
            adminToken,
            new CreateUserRequest
            {
                FullName = fullName,
                Email = email,
                Password = password,
                Role = role,
                DepartmentId = departmentId
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        return new CreatedUser(user, password);
    }

    private async Task<UserDto> InviteTechnicianAsync(
        string adminToken,
        Guid departmentId)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            "/api/users/invitations",
            adminToken,
            new InviteUserRequest(
                "Dashboard Bekleyen Teknisyen",
                $"dashboard-pending-{Guid.NewGuid():N}@example.com",
                UserRole.Technician,
                departmentId));
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        Assert.NotNull(user);
        return user;
    }

    private async Task SetUserStatusAsync(
        string adminToken,
        Guid userId,
        bool isActive)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Patch,
            $"/api/users/{userId}/status",
            adminToken,
            new ChangeUserStatusRequest
            {
                IsActive = isActive
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<TicketDto> CreateTicketAsync(
        string token,
        Guid assetId,
        string title,
        TicketPriority priority)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            "/api/tickets",
            token,
            new CreateTicketRequest
            {
                AssetId = assetId,
                CategoryId = TicketCategory.OtherId,
                Title = title,
                Description = $"{title} açıklaması",
                Priority = priority
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        return ticket;
    }

    private async Task AssignAsync(
        string adminToken,
        Guid ticketId,
        Guid technicianId)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{ticketId}/assignment",
            adminToken,
            new AssignTicketRequest
            {
                TechnicianId = technicianId
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<AssetDto> CreateAssetAsync(
        string adminToken,
        Guid departmentId)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Post,
            "/api/assets",
            adminToken,
            new CreateAssetRequest
            {
                Name = "Dashboard Test Cihazı",
                SerialNumber = $"DASH-{Guid.NewGuid():N}",
                Type = AssetType.Computer,
                DepartmentId = departmentId,
                Location = "Dashboard Test Odası"
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var asset = await response.Content.ReadFromJsonAsync<AssetDto>();
        Assert.NotNull(asset);
        return asset;
    }

    private async Task<Guid> GetDepartmentIdAsync(string adminToken)
    {
        using var request = AuthorizedRequest(
            HttpMethod.Get,
            "/api/departments",
            adminToken);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var departments =
            await response.Content.ReadFromJsonAsync<List<DepartmentDto>>();
        Assert.NotNull(departments);
        var activeDepartment =
            departments.FirstOrDefault(department => department.IsActive);

        Assert.NotNull(activeDepartment);
        return activeDepartment.Id;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest
            {
                Email = email,
                Password = password
            });
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        return login.AccessToken;
    }

    private static HttpRequestMessage AuthorizedRequest(
        HttpMethod method,
        string path,
        string token,
        object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }
        return request;
    }

    private sealed record CreatedUser(UserDto User, string Password);
}
