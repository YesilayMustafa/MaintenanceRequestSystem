using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task CreateTicket_ReturnsConfiguredSlaFields()
    {
        var setup = await CreateTicketSetupAsync();

        Assert.Equal(
            TimeSpan.FromHours(24),
            setup.Ticket.SlaDueAt - setup.Ticket.CreatedAt);
        Assert.Equal(nameof(SlaStatus.OnTrack), setup.Ticket.SlaStatus);
        Assert.InRange(setup.Ticket.SlaRemainingMinutes!.Value, 1439, 1440);
    }

    [Fact]
    public async Task BreachedFilterAndDashboard_RespectEmployeeAndTechnicianScope()
    {
        var setup = await CreateTicketSetupAsync();
        var employee = await CreateEmployeeAsync(setup.AdminToken);
        var employeeToken = await LoginAsync(employee.Email, employee.Password);
        var ticket = await CreateTicketAsync(
            employeeToken,
            setup.Ticket.AssetId,
            "SLA kapsam talebi",
            TicketPriority.High);
        var technician = await CreateTechnicianAsync(setup.AdminToken);
        await AssignTicketAsync(setup.AdminToken, ticket.Id, technician.Id);
        var technicianToken = await LoginAsync(
            technician.Email,
            technician.Password);
        await SetSlaDueAtAsync(ticket.Id, DateTime.UtcNow.AddMinutes(-5));

        var employeeTickets = await GetPagedTicketsAsync(
            employeeToken,
            "/api/tickets?slaStatus=Breached&pageSize=100");
        Assert.Single(employeeTickets.Items);
        Assert.Equal(ticket.Id, employeeTickets.Items[0].Id);
        Assert.Equal(nameof(SlaStatus.Breached), employeeTickets.Items[0].SlaStatus);

        var employeeDashboard = await GetDashboardAsync(employeeToken);
        var technicianDashboard = await GetDashboardAsync(technicianToken);
        Assert.Equal(1, employeeDashboard.SlaBreachedCount);
        Assert.Equal(1, technicianDashboard.SlaBreachedCount);
    }

    private async Task SetSlaDueAtAsync(Guid ticketId, DateTime slaDueAt)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ticket = await context.Tickets.FindAsync(ticketId);
        Assert.NotNull(ticket);
        typeof(Ticket)
            .GetProperty(
                nameof(Ticket.SlaDueAt),
                BindingFlags.Instance | BindingFlags.Public)!
            .SetValue(ticket, slaDueAt);
        await context.SaveChangesAsync();
    }

    private async Task<DashboardDto> GetDashboardAsync(string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/dashboard",
            token);
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>();
        Assert.NotNull(dashboard);
        return dashboard;
    }
}
