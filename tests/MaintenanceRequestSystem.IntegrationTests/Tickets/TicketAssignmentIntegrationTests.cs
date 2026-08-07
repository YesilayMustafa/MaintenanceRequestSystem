using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task ReassignTicket_WithAdminToken_ReturnsReassignedTicket()
    {
        var setup =
            await CreateTicketSetupAsync();

        var firstTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var secondTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = firstTechnician.Id
                });

        var assignResponse =
            await _client.SendAsync(assignRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            assignResponse.StatusCode);

        using var reassignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/reassignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = secondTechnician.Id
                });

        var reassignResponse =
            await _client.SendAsync(reassignRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            reassignResponse.StatusCode);

        var ticket =
            await reassignResponse.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        Assert.Equal(
            "Assigned",
            ticket.Status);

        Assert.Equal(
            secondTechnician.Id,
            ticket.AssignedTechnicianId);

        Assert.Equal(
            "Test Teknik Personeli",
            ticket.AssignedTechnicianFullName);
    }

    [Fact]
    public async Task ReassignTicket_WithEmployeeToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var firstTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var secondTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = firstTechnician.Id
                });

        var assignResponse =
            await _client.SendAsync(assignRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            assignResponse.StatusCode);

        using var reassignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/reassignment",
                setup.EmployeeToken,
                new AssignTicketRequest
                {
                    TechnicianId = secondTechnician.Id
                });

        var response =
            await _client.SendAsync(reassignRequest);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task ReassignTicket_WithSameTechnician_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var requestBody =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        using var assignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                requestBody);

        var assignResponse =
            await _client.SendAsync(assignRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            assignResponse.StatusCode);

        using var reassignRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/reassignment",
                setup.AdminToken,
                requestBody);

        var response =
            await _client.SendAsync(reassignRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithAdminToken_ReturnsAssignedTicket()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var requestBody =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var assignedTicket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(assignedTicket);

        Assert.Equal(
            "Assigned",
            assignedTicket.Status);

        Assert.Equal(
            technician.Id,
            assignedTicket.AssignedTechnicianId);

        Assert.Equal(
            "Test Teknik Personeli",
            assignedTicket.AssignedTechnicianFullName);

        Assert.NotNull(
            assignedTicket.UpdatedAt);
    }

    [Fact]
    public async Task AssignTicket_WithAdminToken_CreatesTicketHistory()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = technician.Id
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var adminToken =
            new JwtSecurityTokenHandler()
                .ReadJwtToken(setup.AdminToken);

        var adminId =
            Guid.Parse(adminToken.Subject);

        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var history =
            await dbContext.TicketHistories
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    item =>
                        item.TicketId ==
                        setup.Ticket.Id);

        Assert.NotNull(history);

        Assert.Equal(
            TicketStatus.Open,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Assigned,
            history.NewStatus);

        Assert.Equal(
            adminId,
            history.PerformedByUserId);
    }

    [Fact]
    public async Task AssignTicket_WithEmployeeToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var requestBody =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.EmployeeToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithTechnicianToken_ReturnsForbidden()
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

        var requestBody =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                technicianToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WhenAlreadyAssigned_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var requestBody =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        using var firstRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                requestBody);

        var firstResponse =
            await _client.SendAsync(firstRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode);

        using var secondRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                requestBody);

        var secondResponse =
            await _client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithoutToken_ReturnsUnauthorized()
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/tickets/{Guid.NewGuid()}/assignment")
            {
                Content =
                    JsonContent.Create(
                        new AssignTicketRequest
                        {
                            TechnicianId = Guid.NewGuid()
                        })
            };

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithMissingTechnician_ReturnsNotFound()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = Guid.NewGuid()
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithMissingTicket_ReturnsNotFound()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{Guid.NewGuid()}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = technician.Id
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithInactiveTechnician_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        using var statusRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/users/{technician.Id}/status",
                setup.AdminToken,
                new ChangeUserStatusRequest
                {
                    IsActive = false
                });

        var statusResponse =
            await _client.SendAsync(statusRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            statusResponse.StatusCode);

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
            HttpStatusCode.BadRequest,
            assignResponse.StatusCode);
    }

    [Fact]
    public async Task AssignTicket_WithEmployeeAsTarget_ReturnsBadRequest()
    {
        var setup =
            await CreateTicketSetupAsync();

        var employee =
            await CreateEmployeeAsync(
                setup.AdminToken);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/assignment",
                setup.AdminToken,
                new AssignTicketRequest
                {
                    TechnicianId = employee.Id
                });

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

}
