using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task GetTimeline_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            "/api/tickets/timeline" +
            "?from=2040-01-01T00:00:00.000Z" +
            "&to=2040-01-07T23:59:59.999Z");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetTimeline_WithEmployeeToken_PreservesRoleScope()
    {
        var setup = await CreateTicketSetupAsync();
        var secondEmployee = await CreateEmployeeAsync(setup.AdminToken);
        var secondEmployeeToken = await LoginAsync(
            secondEmployee.Email,
            secondEmployee.Password);

        var otherTicket = await CreateTicketAsync(
            secondEmployeeToken,
            setup.Ticket.AssetId,
            "Başka çalışanın timeline talebi",
            TicketPriority.Medium);

        var createdAt = new DateTime(2020, 1, 3, 9, 0, 0, DateTimeKind.Utc);
        await SetTimelineDatesAsync(
            setup.Ticket.Id,
            TicketStatus.Open,
            createdAt,
            createdAt.AddHours(48));
        await SetTimelineDatesAsync(
            otherTicket.Id,
            TicketStatus.Open,
            createdAt.AddHours(1),
            createdAt.AddHours(49));

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            GetTimelineUri(
                new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2020, 1, 7, 23, 59, 59, DateTimeKind.Utc)),
            setup.EmployeeToken);

        var response = await _client.SendAsync(request);
        var items = await response.Content
            .ReadFromJsonAsync<List<TicketTimelineItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.Contains(items, item => item.Id == setup.Ticket.Id);
        Assert.DoesNotContain(items, item => item.Id == otherTicket.Id);
    }

    [Fact]
    public async Task GetTimeline_ReturnsOverlapsAndExcludesOutsideRange()
    {
        var setup = await CreateTicketSetupAsync();
        var overlappingTicket = await CreateTicketAsync(
            setup.EmployeeToken,
            setup.Ticket.AssetId,
            "Aralıkla kesişen talep",
            TicketPriority.High);
        var outsideTicket = await CreateTicketAsync(
            setup.EmployeeToken,
            setup.Ticket.AssetId,
            "Aralık dışında kapanan talep",
            TicketPriority.Low);
        var futureTicket = await CreateTicketAsync(
            setup.EmployeeToken,
            setup.Ticket.AssetId,
            "Aralıktan sonra açılan talep",
            TicketPriority.Medium);
        var boundaryTicket = await CreateTicketAsync(
            setup.EmployeeToken,
            setup.Ticket.AssetId,
            "SLA boundary ticket",
            TicketPriority.Critical);

        await SetTimelineDatesAsync(
            overlappingTicket.Id,
            TicketStatus.Resolved,
            new DateTime(2039, 12, 29, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2040, 1, 2, 8, 0, 0, DateTimeKind.Utc),
            resolvedAt: new DateTime(2040, 1, 2, 12, 0, 0, DateTimeKind.Utc));
        await SetTimelineDatesAsync(
            outsideTicket.Id,
            TicketStatus.Closed,
            new DateTime(2039, 12, 1, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2039, 12, 20, 12, 0, 0, DateTimeKind.Utc),
            closedAt: new DateTime(2039, 12, 20, 12, 0, 0, DateTimeKind.Utc));
        await SetTimelineDatesAsync(
            futureTicket.Id,
            TicketStatus.Open,
            new DateTime(2040, 1, 8, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2040, 1, 10, 8, 0, 0, DateTimeKind.Utc));
        await SetTimelineDatesAsync(
            boundaryTicket.Id,
            TicketStatus.Open,
            new DateTime(2039, 12, 30, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            GetTimelineUri(
                new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2040, 1, 7, 23, 59, 59, DateTimeKind.Utc)),
            setup.AdminToken);

        var response = await _client.SendAsync(request);
        var items = await response.Content
            .ReadFromJsonAsync<List<TicketTimelineItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        Assert.Contains(items, item => item.Id == overlappingTicket.Id);
        Assert.Contains(items, item => item.Id == boundaryTicket.Id);
        Assert.DoesNotContain(items, item => item.Id == outsideTicket.Id);
        Assert.DoesNotContain(items, item => item.Id == futureTicket.Id);
    }

    [Fact]
    public async Task GetTimeline_ForTicketCreatedToday_ReturnsFortyEightHourSlaWindow()
    {
        var setup = await CreateTicketSetupAsync();
        var ticket = await CreateTicketAsync(
            setup.EmployeeToken,
            setup.Ticket.AssetId,
            "Forty eight hour SLA window",
            TicketPriority.Medium);
        var utcNow = DateTime.UtcNow;
        var createdAt = new DateTime(
            utcNow.Ticks / TimeSpan.TicksPerSecond * TimeSpan.TicksPerSecond,
            DateTimeKind.Utc);
        var slaDueAt = createdAt.AddHours(48);

        await SetTimelineDatesAsync(
            ticket.Id,
            TicketStatus.Open,
            createdAt,
            slaDueAt);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            GetTimelineUri(createdAt.AddHours(-1), slaDueAt.AddHours(1)),
            setup.EmployeeToken);

        var response = await _client.SendAsync(request);
        var items = await response.Content
            .ReadFromJsonAsync<List<TicketTimelineItemDto>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(items);
        var result = Assert.Single(items, item => item.Id == ticket.Id);
        Assert.Equal(createdAt, result.CreatedAt);
        Assert.Equal(slaDueAt, result.SlaDueAt);
    }

    [Fact]
    public async Task GetTimeline_WhenRangeIsInvalid_ReturnsBadRequest()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            GetTimelineUri(
                new DateTime(2040, 1, 8, 0, 0, 0, DateTimeKind.Utc),
                new DateTime(2040, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            adminToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string GetTimelineUri(DateTime from, DateTime to)
    {
        return "/api/tickets/timeline" +
            $"?from={Uri.EscapeDataString(from.ToString("O"))}" +
            $"&to={Uri.EscapeDataString(to.ToString("O"))}";
    }

    private async Task SetTimelineDatesAsync(
        Guid ticketId,
        TicketStatus status,
        DateTime createdAt,
        DateTime slaDueAt,
        DateTime? resolvedAt = null,
        DateTime? closedAt = null)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();
        var ticket = await context.Tickets
            .SingleAsync(item => item.Id == ticketId);

        SetTimelineProperty(ticket, nameof(Ticket.Status), status);
        SetTimelineProperty(ticket, nameof(Ticket.CreatedAt), createdAt);
        SetTimelineProperty(ticket, nameof(Ticket.SlaDueAt), slaDueAt);
        SetTimelineProperty(ticket, nameof(Ticket.ResolvedAt), resolvedAt);
        SetTimelineProperty(ticket, nameof(Ticket.ClosedAt), closedAt);
        SetTimelineProperty(
            ticket,
            nameof(Ticket.UpdatedAt),
            closedAt ?? resolvedAt ?? createdAt);

        await context.SaveChangesAsync();
    }

    private static void SetTimelineProperty<T>(
        Ticket ticket,
        string propertyName,
        T value)
    {
        typeof(Ticket)
            .GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(ticket, value);
    }
}
