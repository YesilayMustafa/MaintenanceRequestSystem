using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task SoftDelete_ByAdmin_WhenCancelled_HidesTicketFromDetailQuery()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var cancelRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/cancel",
                setup.EmployeeToken);

        var cancelResponse =
            await _client.SendAsync(cancelRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            cancelResponse.StatusCode);

        using var deleteRequest =
            CreateAuthorizedRequest(
                HttpMethod.Delete,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var deleteResponse =
            await _client.SendAsync(deleteRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        using var detailRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var detailResponse =
            await _client.SendAsync(detailRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            detailResponse.StatusCode);

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
                        item.Action == "TicketSoftDeleted");

        Assert.NotNull(auditLog);


        Assert.Equal(
            nameof(Ticket),
            auditLog.EntityName);

        Assert.Equal(
            setup.Ticket.Id.ToString(),
            auditLog.EntityId);

        Assert.Equal(
            "TicketSoftDeleted",
            auditLog.Action);

        Assert.NotNull(
            auditLog.OldValues);

        Assert.NotNull(
            auditLog.NewValues);
    }

    [Fact]
    public async Task SoftDelete_ByAdmin_WhenClosed_ReturnsNoContent()
    {
        var setup =
            await CreateClosedTicketSetupAsync();

        using var deleteRequest =
            CreateAuthorizedRequest(
                HttpMethod.Delete,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var response =
            await _client.SendAsync(deleteRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        using var detailRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var detailResponse =
            await _client.SendAsync(detailRequest);

        Assert.Equal(
            HttpStatusCode.NotFound,
            detailResponse.StatusCode);
    }

    [Fact]
    public async Task SoftDelete_ByEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Delete,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task SoftDelete_WhenTicketIsOpen_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Delete,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

}
