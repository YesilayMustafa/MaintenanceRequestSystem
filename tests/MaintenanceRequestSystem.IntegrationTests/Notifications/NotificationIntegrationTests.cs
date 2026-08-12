using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Notifications.Dtos;
using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Notifications;

public sealed class NotificationIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private const string TechnicianPassword = "TechnicianTest123!";
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public NotificationIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AssignTicket_CreatesNotificationForTargetTechnician()
    {
        var setup = await SeedTicketAsync(assignToTechnician: false);
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{setup.TicketId}/assignment",
            adminToken,
            new AssignTicketRequest { TechnicianId = setup.TechnicianId });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertNotificationAsync(
            setup.TechnicianId,
            setup.TicketId,
            NotificationType.TicketAssigned);
    }

    [Fact]
    public async Task StartProgress_CreatesNotificationForTicketCreator()
    {
        var setup = await SeedTicketAsync(assignToTechnician: true);
        var technicianToken = await LoginAsync(
            setup.TechnicianEmail,
            TechnicianPassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{setup.TicketId}/start-progress",
            technicianToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertNotificationAsync(
            setup.CreatorId,
            setup.TicketId,
            NotificationType.TicketStatusChanged);
    }

    [Fact]
    public async Task CreatorComment_CreatesSingleNotificationForTechnician()
    {
        var setup = await SeedTicketAsync(assignToTechnician: true);
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/tickets/{setup.TicketId}/comments",
            employeeToken,
            new CreateTicketCommentRequest { Content = "Durum nedir?" });
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notifications = await context.Notifications
            .Where(notification => notification.TicketId == setup.TicketId)
            .ToListAsync();
        var notification = Assert.Single(notifications);
        Assert.Equal(setup.TechnicianId, notification.UserId);
        Assert.Equal(NotificationType.TicketCommentAdded, notification.Type);
    }

    [Fact]
    public async Task GetNotifications_ReturnsOnlyCurrentUsersNotificationsInOrder()
    {
        var users = await SeedInboxNotificationsAsync();
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/notifications?pageNumber=1&pageSize=1",
            employeeToken);
        var response = await _client.SendAsync(request);
        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<NotificationDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.DoesNotContain(
            result.Items,
            notification => notification.Message.Contains(users.OtherUserId.ToString()));
    }

    [Fact]
    public async Task MarkRead_ForAnotherUsersNotification_ReturnsNotFound()
    {
        var users = await SeedInboxNotificationsAsync();
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/notifications/{users.OtherNotificationId}/read",
            employeeToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_IsIdempotentAndUpdatesUnreadCount()
    {
        var users = await SeedInboxNotificationsAsync();
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        for (var call = 0; call < 2; call++)
        {
            using var request = CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/notifications/{users.OwnNotificationId}/read",
                employeeToken);
            var response = await _client.SendAsync(request);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        using var countRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/notifications/unread-count",
            employeeToken);
        var countResponse = await _client.SendAsync(countRequest);
        var count = await countResponse.Content
            .ReadFromJsonAsync<UnreadNotificationCountDto>();

        Assert.NotNull(count);
        Assert.Equal(1, count.Count);
    }

    [Fact]
    public async Task MarkAllRead_OnlyUpdatesCurrentUsersNotifications()
    {
        var users = await SeedInboxNotificationsAsync();
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            "/api/notifications/read-all",
            employeeToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Assert.All(
            await context.Notifications
                .Where(notification => notification.UserId == users.EmployeeId)
                .ToListAsync(),
            notification => Assert.True(notification.IsRead));
        Assert.False((await context.Notifications.FindAsync(
            users.OtherNotificationId))!.IsRead);
    }

    [Fact]
    public async Task GetNotifications_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<TicketSetup> SeedTicketAsync(bool assignToTechnician)
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
        var technicianEmail = $"technician-{Guid.NewGuid():N}@example.com";
        var technician = new User(
            "Bildirim Teknisyeni",
            technicianEmail,
            passwordHasher.HashPassword(TechnicianPassword),
            UserRole.Technician,
            department.Id);
        var asset = new Asset(
            "Bildirim Test Cihazı",
            $"NTF-{Guid.NewGuid():N}",
            AssetType.Computer,
            department.Id);
        var ticket = new Ticket(
            $"REQ-2026-{Random.Shared.Next(1, 999999):D6}",
            asset.Id,
            employee.Id,
            "Bildirim testi",
            "Bildirim integration testi için talep.",
            TicketPriority.Medium);

        if (assignToTechnician)
        {
            ticket.Assign(technician.Id, admin.Id);
        }

        context.Notifications.RemoveRange(context.Notifications);
        await context.Users.AddAsync(technician);
        await context.Assets.AddAsync(asset);
        await context.Tickets.AddAsync(ticket);
        await context.SaveChangesAsync();

        return new TicketSetup(
            ticket.Id,
            employee.Id,
            technician.Id,
            technicianEmail);
    }

    private async Task<InboxSetup> SeedInboxNotificationsAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var employee = await context.Users.SingleAsync(user =>
            user.Email == CustomWebApplicationFactory.EmployeeEmail);
        var admin = await context.Users.SingleAsync(user =>
            user.Email == CustomWebApplicationFactory.AdminEmail);

        context.Notifications.RemoveRange(context.Notifications);
        var first = new Notification(
            employee.Id,
            NotificationType.TicketAssigned,
            "Birinci",
            "Birinci bildirim");
        var second = new Notification(
            employee.Id,
            NotificationType.TicketResolved,
            "İkinci",
            "İkinci bildirim");
        var other = new Notification(
            admin.Id,
            NotificationType.TicketClosed,
            "Diğer",
            $"Diğer kullanıcı {admin.Id}");
        await context.Notifications.AddRangeAsync(first, second, other);
        await context.SaveChangesAsync();

        return new InboxSetup(
            employee.Id,
            admin.Id,
            first.Id,
            other.Id);
    }

    private async Task AssertNotificationAsync(
        Guid userId,
        Guid ticketId,
        NotificationType type)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var notification = await context.Notifications.SingleAsync(item =>
            item.UserId == userId &&
            item.TicketId == ticketId);
        Assert.Equal(type, notification.Type);
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
        string token,
        object? content = null)
    {
        var request = new HttpRequestMessage(method, uri);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        if (content is not null)
        {
            request.Content = JsonContent.Create(content);
        }

        return request;
    }

    private sealed record TicketSetup(
        Guid TicketId,
        Guid CreatorId,
        Guid TechnicianId,
        string TechnicianEmail);

    private sealed record InboxSetup(
        Guid EmployeeId,
        Guid OtherUserId,
        Guid OwnNotificationId,
        Guid OtherNotificationId);
}
