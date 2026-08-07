using MaintenanceRequestSystem.Application.Tickets.Dtos;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task PutOnHold_ByAssignedTechnician_ReturnsWaitingTicket()
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

        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.SendAsync(assignRequest)).StatusCode);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.SendAsync(startRequest)).StatusCode);

        using var holdRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/put-on-hold",
                technicianToken,
                new PutTicketOnHoldRequest
                {
                    Reason = "Yedek parça bekleniyor."
                });

        var response =
            await _client.SendAsync(holdRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);
        Assert.Equal("Waiting", ticket.Status);

        Assert.Equal(
            "Yedek parça bekleniyor.",
            ticket.WaitingReason);
    }

    [Fact]
    public async Task Resume_ByAssignedTechnician_ReturnsInProgressTicket()
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

        await _client.SendAsync(assignRequest);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        await _client.SendAsync(startRequest);

        using var holdRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/put-on-hold",
                technicianToken,
                new PutTicketOnHoldRequest
                {
                    Reason = "Onay bekleniyor."
                });

        await _client.SendAsync(holdRequest);

        using var resumeRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resume",
                technicianToken);

        var response =
            await _client.SendAsync(resumeRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);
        Assert.Equal("InProgress", ticket.Status);
        Assert.Null(ticket.WaitingReason);
    }

    [Fact]
    public async Task PutOnHold_ByEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/put-on-hold",
                setup.EmployeeToken,
                new PutTicketOnHoldRequest
                {
                    Reason = "Test"
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task PutOnHold_ByDifferentTechnician_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var differentTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = assignedTechnician.Id
                });

        await _client.SendAsync(assignRequest);

        var assignedToken =
            await LoginAsync(
                assignedTechnician.Email,
                assignedTechnician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                assignedToken);

        await _client.SendAsync(startRequest);

        var differentToken =
            await LoginAsync(
                differentTechnician.Email,
                differentTechnician.Password);

        using var holdRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/put-on-hold",
                differentToken,
                new PutTicketOnHoldRequest
                {
                    Reason = "Test"
                });

        var response =
            await _client.SendAsync(holdRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Resolve_ByAssignedTechnician_ReturnsResolvedTicket()
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

        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.SendAsync(assignRequest)).StatusCode);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        Assert.Equal(
            HttpStatusCode.OK,
            (await _client.SendAsync(startRequest)).StatusCode);

        using var resolveRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resolve",
                technicianToken,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Ağ yapılandırması düzeltildi."
                });

        var response =
            await _client.SendAsync(resolveRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);
        Assert.Equal("Resolved", ticket.Status);

        Assert.Equal(
            "Ağ yapılandırması düzeltildi.",
            ticket.ResolutionDescription);

        Assert.NotNull(ticket.ResolvedAt);
    }

    [Fact]
    public async Task Resolve_ByEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resolve",
                setup.EmployeeToken,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Sorun çözüldü."
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Resolve_ByDifferentTechnician_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var differentTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = assignedTechnician.Id
                });

        await _client.SendAsync(assignRequest);

        var assignedToken =
            await LoginAsync(
                assignedTechnician.Email,
                assignedTechnician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                assignedToken);

        await _client.SendAsync(startRequest);

        var differentToken =
            await LoginAsync(
                differentTechnician.Email,
                differentTechnician.Password);

        using var resolveRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resolve",
                differentToken,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Sorun çözüldü."
                });

        var response =
            await _client.SendAsync(resolveRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Resolve_WhenTicketIsWaiting_ReturnsBadRequest()
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

        await _client.SendAsync(assignRequest);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var startRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        await _client.SendAsync(startRequest);

        using var holdRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/put-on-hold",
                technicianToken,
                new PutTicketOnHoldRequest
                {
                    Reason = "Parça bekleniyor."
                });

        await _client.SendAsync(holdRequest);

        using var resolveRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resolve",
                technicianToken,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Sorun çözüldü."
                });

        var response =
            await _client.SendAsync(resolveRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task StartProgress_ByAssignedTechnician_ReturnsInProgressTicket()
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

        using var progressRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                technicianToken);

        var response =
            await _client.SendAsync(progressRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        Assert.Equal(
            "InProgress",
            ticket.Status);

        Assert.Equal(
            technician.Id,
            ticket.AssignedTechnicianId);
    }

    [Fact]
    public async Task StartProgress_ByEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Close_ByTicketCreator_ReturnsClosedTicket()
    {
        var setup =
            await CreateResolvedTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/close",
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
        Assert.Equal("Closed", ticket.Status);
        Assert.NotNull(ticket.ClosedAt);
    }

    [Fact]
    public async Task Close_ByAdmin_ReturnsClosedTicket()
    {
        var setup =
            await CreateResolvedTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/close",
                setup.AdminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);
        Assert.Equal("Closed", ticket.Status);
    }

    [Fact]
    public async Task Close_ByDifferentEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateResolvedTicketSetupAsync();

        var secondEmployee =
            await CreateEmployeeAsync(
                setup.AdminToken);

        var secondEmployeeToken =
            await LoginAsync(
                secondEmployee.Email,
                secondEmployee.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/close",
                secondEmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Close_ByTechnician_ReturnsForbidden()
    {
        var setup =
            await CreateResolvedTicketSetupAsync();

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
                $"/api/tickets/{setup.Ticket.Id}/close",
                technicianToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task StartProgress_ByDifferentTechnician_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var differentTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = assignedTechnician.Id
                });

        var assignResponse =
            await _client.SendAsync(assignRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            assignResponse.StatusCode);

        var differentTechnicianToken =
            await LoginAsync(
                differentTechnician.Email,
                differentTechnician.Password);

        using var progressRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/start-progress",
                differentTechnicianToken);

        var response =
            await _client.SendAsync(progressRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task Reopen_ByTicketCreator_ReturnsInProgressTicket()
    {
        var setup =
            await CreateClosedTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/reopen",
                setup.EmployeeToken,
                new ReopenTicketRequest
                {
                    Reason = "Sorun yeniden oluştu."
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
        Assert.Equal("InProgress", ticket.Status);

        Assert.Null(
            ticket.ResolutionDescription);

        Assert.Null(
            ticket.ResolvedAt);

        Assert.Null(
            ticket.ClosedAt);
    }

    [Fact]
    public async Task Reopen_ByAdmin_ReturnsInProgressTicket()
    {
        var setup =
            await CreateClosedTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/reopen",
                setup.AdminToken,
                new ReopenTicketRequest
                {
                    Reason = "Çözüm yeterli olmadı."
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
        Assert.Equal("InProgress", ticket.Status);
    }

    [Fact]
    public async Task Reopen_ByDifferentEmployee_ReturnsForbidden()
    {
        var setup =
            await CreateClosedTicketSetupAsync();

        var secondEmployee =
            await CreateEmployeeAsync(
                setup.AdminToken);

        var secondEmployeeToken =
            await LoginAsync(
                secondEmployee.Email,
                secondEmployee.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/reopen",
                secondEmployeeToken,
                new ReopenTicketRequest
                {
                    Reason = "Sorun devam ediyor."
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task Reopen_ByTechnician_ReturnsForbidden()
    {
        var setup =
            await CreateClosedTicketSetupAsync();

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
                $"/api/tickets/{setup.Ticket.Id}/reopen",
                technicianToken,
                new ReopenTicketRequest
                {
                    Reason = "Sorun devam ediyor."
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

}
