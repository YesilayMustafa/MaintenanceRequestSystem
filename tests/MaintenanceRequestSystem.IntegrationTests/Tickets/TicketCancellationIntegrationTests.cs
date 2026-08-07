using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task Cancel_ByTicketCreator_WhenOpen_ReturnsCancelledTicket()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/cancel",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        Assert.Equal(
            "Cancelled",
            ticket.Status);

        using var scope =
    _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var auditLog =
            await dbContext.AuditLogs
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.EntityName == nameof(Ticket) &&
                        item.EntityId == setup.Ticket.Id.ToString() &&
                        item.Action == "TicketCancelled");

        Assert.NotNull(auditLog);

        Assert.Equal(
            setup.Ticket.Id.ToString(),
            auditLog.EntityId);

        Assert.Equal(
            "TicketCancelled",
            auditLog.Action);

        Assert.NotNull(
            auditLog.OldValues);

        Assert.NotNull(
            auditLog.NewValues);
    }

    [Fact]
    public async Task Cancel_ByAdmin_WhenAssigned_ReturnsCancelledTicket()
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

        using var cancelRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/cancel",
                setup.AdminToken);

        var response =
            await _client.SendAsync(cancelRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        Assert.Equal(
            "Cancelled",
            ticket.Status);
    }

    [Fact]
    public async Task Cancel_ByTicketCreator_WhenAssigned_ReturnsForbidden()
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

        using var cancelRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/cancel",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(cancelRequest);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Cancel_ByTechnician_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/cancel",
                technicianToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

}
