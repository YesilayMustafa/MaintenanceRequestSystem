using MaintenanceRequestSystem.Application.Tickets.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task GetHistory_ByAdmin_ReturnsStatusTransitions()
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

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        var startResponse =
            await _client.SendAsync(startRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            startResponse.StatusCode);

        using var historyRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/history",
                setup.AdminToken);

        var response =
            await _client.SendAsync(historyRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var histories =
            await response.Content
                .ReadFromJsonAsync<List<TicketHistoryDto>>();

        Assert.NotNull(histories);

        Assert.Contains(
            histories,
            history =>
                history.OldStatus == "Open" &&
                history.NewStatus == "Assigned");

        Assert.Contains(
            histories,
            history =>
                history.OldStatus == "Assigned" &&
                history.NewStatus == "InProgress");
    }

    [Fact]
    public async Task GetHistory_ByAssignedTechnician_ReturnsOk()
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

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var historyRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}/history",
                technicianToken);

        var response =
            await _client.SendAsync(historyRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var histories =
            await response.Content
                .ReadFromJsonAsync<List<TicketHistoryDto>>();

        Assert.NotNull(histories);
        Assert.NotEmpty(histories);

        Assert.Contains(
            histories,
            history =>
                history.NewStatus == "Assigned");
    }

    [Fact]
    public async Task GetHistory_ByDifferentEmployee_ReturnsForbidden()
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
                $"/api/tickets/{setup.Ticket.Id}/history",
                secondEmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

}
