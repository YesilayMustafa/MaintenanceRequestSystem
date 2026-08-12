using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Assets;

public sealed class AssetMaintenanceHistoryIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string UserPassword = "HistoryTest123!";
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AssetMaintenanceHistoryIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHistory_ByAdmin_ReturnsSummaryAndPagedNewestTickets()
    {
        var setup = await SeedHistoryAsync();
        var token = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        var result = await GetHistoryAsync(setup.AssetId, token, pageSize: 2);

        Assert.Equal(4, result.Summary.TotalTicketCount);
        Assert.Equal(4, result.Summary.ActiveTicketCount);
        Assert.Equal(1, result.Summary.CriticalTicketCount);
        Assert.Equal(4, result.Tickets.TotalCount);
        Assert.Equal(2, result.Tickets.Items.Count);
        Assert.True(
            result.Tickets.Items[0].CreatedAt >=
            result.Tickets.Items[1].CreatedAt);
        Assert.All(result.Tickets.Items, ticket =>
        {
            Assert.StartsWith("REQ-2026-", ticket.TicketNumber);
            Assert.False(string.IsNullOrWhiteSpace(ticket.CategoryName));
            Assert.False(string.IsNullOrWhiteSpace(ticket.Status));
            Assert.False(string.IsNullOrWhiteSpace(ticket.Priority));
        });
    }

    [Fact]
    public async Task GetHistory_ByEmployee_ReturnsOnlyOwnAssetTickets()
    {
        var setup = await SeedHistoryAsync();
        var token = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        var result = await GetHistoryAsync(setup.AssetId, token);

        var ticket = Assert.Single(result.Tickets.Items);
        Assert.Equal(setup.EmployeeTicketId, ticket.Id);
        Assert.Equal(1, result.Summary.TotalTicketCount);
    }

    [Fact]
    public async Task GetHistory_ByTechnician_ReturnsOnlyAssignedAssetTickets()
    {
        var setup = await SeedHistoryAsync();
        var token = await LoginAsync(setup.TechnicianEmail, UserPassword);

        var result = await GetHistoryAsync(setup.AssetId, token);

        var ticket = Assert.Single(result.Tickets.Items);
        Assert.Equal(setup.TechnicianTicketId, ticket.Id);
        Assert.Equal(setup.TechnicianFullName, ticket.AssignedTechnicianFullName);
        Assert.Equal(1, result.Summary.TotalTicketCount);
    }

    [Fact]
    public async Task GetHistory_DoesNotIncludeAnotherAssetsTickets()
    {
        var setup = await SeedHistoryAsync();
        var token = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        var result = await GetHistoryAsync(setup.AssetId, token);

        Assert.DoesNotContain(
            result.Tickets.Items,
            ticket => ticket.Id == setup.OtherAssetTicketId);
    }

    [Fact]
    public async Task GetHistory_ForNonexistentAsset_ReturnsNotFound()
    {
        var token = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/assets/{Guid.NewGuid()}/maintenance-history",
            token);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetHistory_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            $"/api/assets/{Guid.NewGuid()}/maintenance-history");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<HistorySetup> SeedHistoryAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHashService>();
        var employee = await context.Users.SingleAsync(user =>
            user.Email == CustomWebApplicationFactory.EmployeeEmail);
        var admin = await context.Users.SingleAsync(user =>
            user.Email == CustomWebApplicationFactory.AdminEmail);
        var department = await context.Departments.FirstAsync();
        var category = await context.TicketCategories.FirstAsync();
        var suffix = Guid.NewGuid().ToString("N");
        var otherEmployee = CreateUser(
            "Diğer Çalışan",
            $"other-{suffix}@example.com",
            UserRole.Employee,
            department.Id,
            passwordHasher);
        var technician = CreateUser(
            "Birinci Teknisyen",
            $"technician-{suffix}@example.com",
            UserRole.Technician,
            department.Id,
            passwordHasher);
        var otherTechnician = CreateUser(
            "İkinci Teknisyen",
            $"technician-other-{suffix}@example.com",
            UserRole.Technician,
            department.Id,
            passwordHasher);
        var asset = new Asset(
            "Geçmiş Test Cihazı",
            $"HISTORY-{suffix}",
            AssetType.Server,
            department.Id);
        var otherAsset = new Asset(
            "Diğer Cihaz",
            $"OTHER-{suffix}",
            AssetType.Printer,
            department.Id);
        var employeeTicket = CreateTicket(
            asset.Id,
            category.Id,
            employee.Id,
            "Çalışan talebi",
            TicketPriority.Critical);
        var otherEmployeeTicket = CreateTicket(
            asset.Id,
            category.Id,
            otherEmployee.Id,
            "Diğer çalışan talebi",
            TicketPriority.Low);
        var technicianTicket = CreateTicket(
            asset.Id,
            category.Id,
            otherEmployee.Id,
            "Birinci teknisyen talebi",
            TicketPriority.High);
        technicianTicket.Assign(technician.Id, admin.Id);
        var otherTechnicianTicket = CreateTicket(
            asset.Id,
            category.Id,
            otherEmployee.Id,
            "İkinci teknisyen talebi",
            TicketPriority.Medium);
        otherTechnicianTicket.Assign(otherTechnician.Id, admin.Id);
        var otherAssetTicket = CreateTicket(
            otherAsset.Id,
            category.Id,
            employee.Id,
            "Başka cihaz talebi",
            TicketPriority.Medium);

        await context.Users.AddRangeAsync(
            otherEmployee,
            technician,
            otherTechnician);
        await context.Assets.AddRangeAsync(asset, otherAsset);
        await context.Tickets.AddRangeAsync(
            employeeTicket,
            otherEmployeeTicket,
            technicianTicket,
            otherTechnicianTicket,
            otherAssetTicket);
        await context.SaveChangesAsync();

        return new HistorySetup(
            asset.Id,
            employeeTicket.Id,
            technicianTicket.Id,
            otherAssetTicket.Id,
            technician.Email,
            technician.FullName);
    }

    private static User CreateUser(
        string name,
        string email,
        UserRole role,
        Guid departmentId,
        IPasswordHashService passwordHasher)
    {
        return new User(
            name,
            email,
            passwordHasher.HashPassword(UserPassword),
            role,
            departmentId);
    }

    private static Ticket CreateTicket(
        Guid assetId,
        Guid categoryId,
        Guid creatorId,
        string title,
        TicketPriority priority)
    {
        return new Ticket(
            $"REQ-2026-{Random.Shared.Next(1, 999999):D6}",
            assetId,
            categoryId,
            creatorId,
            title,
            "Asset maintenance history integration testi.",
            priority);
    }

    private async Task<AssetMaintenanceHistoryDto> GetHistoryAsync(
        Guid assetId,
        string token,
        int pageSize = 10)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/assets/{assetId}/maintenance-history?pageNumber=1&pageSize={pageSize}",
            token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content
            .ReadFromJsonAsync<AssetMaintenanceHistoryDto>();
        Assert.NotNull(result);
        return result;
    }

    private async Task<string> LoginAsync(string email, string password)
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginRequest { Email = email, Password = password });
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(result);
        return result.AccessToken;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string uri,
        string token)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private sealed record HistorySetup(
        Guid AssetId,
        Guid EmployeeTicketId,
        Guid TechnicianTicketId,
        Guid OtherAssetTicketId,
        string TechnicianEmail,
        string TechnicianFullName);
}
