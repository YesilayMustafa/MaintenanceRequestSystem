using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task ChangePriority_ByAdmin_ReturnsUpdatedTicket()
    {
        var setup =
            await CreateTicketSetupAsync();

        var newPriority =
            setup.Ticket.Priority == nameof(TicketPriority.Critical)
                ? TicketPriority.Low
                : TicketPriority.Critical;

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/priority",
                setup.AdminToken,
                new ChangeTicketPriorityRequest
                {
                    Priority = newPriority
                });

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
            newPriority.ToString(),
            ticket.Priority);
    }

    [Fact]
    public async Task ChangePriority_ByEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/priority",
                setup.EmployeeToken,
                new ChangeTicketPriorityRequest
                {
                    Priority = TicketPriority.Critical
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_WithSamePriority_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var currentPriority =
            Enum.Parse<TicketPriority>(
                setup.Ticket.Priority);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/priority",
                setup.AdminToken,
                new ChangeTicketPriorityRequest
                {
                    Priority = currentPriority
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangePriority_WhenTicketIsCancelled_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var cancelRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/cancel",
                setup.EmployeeToken);

        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.SendAsync(cancelRequest)).StatusCode);

        using var priorityRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/priority",
                setup.AdminToken,
                new ChangeTicketPriorityRequest
                {
                    Priority = TicketPriority.Critical
                });

        var response =
            await _client.SendAsync(priorityRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

}
