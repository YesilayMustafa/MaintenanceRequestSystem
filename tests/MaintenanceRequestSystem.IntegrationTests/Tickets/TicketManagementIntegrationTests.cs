using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;


namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed class TicketManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;
    public TicketManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }


    [Fact]
    public async Task CreateTicket_WithoutToken_ReturnsUnauthorized()
    {
        var response =
            await _client.PostAsJsonAsync(
                "/api/tickets",
                new CreateTicketRequest());

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithOverflowingOffset_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/tickets" +
                "?pageNumber=2147483647" +
                "&pageSize=100",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithUnsupportedRoleToken_ReturnsForbidden()
    {
        var accessToken =
            CreateTokenWithRole("999");

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/tickets?pageNumber=1&pageSize=10",
                accessToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

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
    public async Task GetTickets_WithPagination_ReturnsCorrectPages()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Pagination Talebi 1",
            TicketPriority.Low);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Pagination Talebi 2",
            TicketPriority.Medium);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Pagination Talebi 3",
            TicketPriority.High);

        var firstPage =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=1&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Equal(1, firstPage.PageNumber);
        Assert.Equal(2, firstPage.PageSize);

        var secondPage =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=2&pageSize=2");

        Assert.Single(secondPage.Items);
        Assert.Equal(2, secondPage.PageNumber);
        Assert.Equal(3, secondPage.TotalCount);
        Assert.Equal(2, secondPage.TotalPages);
    }
    [Fact]
    public async Task GetTickets_SortedByTitleAscending_ReturnsOrderedItems()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "C Talebi",
            TicketPriority.Low);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "A Talebi",
            TicketPriority.Low);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "B Talebi",
            TicketPriority.Low);

        var result =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=1&pageSize=20" +
                "&sortBy=title&sortDescending=false");

        var titles =
            result.Items
                .Select(ticket => ticket.Title)
                .ToList();

        Assert.Equal(
            new[]
            {
            "A Talebi",
            "B Talebi",
            "C Talebi"
            },
            titles);
    }

    [Fact]
    public async Task GetTickets_WithInvalidPageSize_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/tickets?pageNumber=1&pageSize=101",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithFilters_ReturnsMatchingTickets()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var highTicket =
            await CreateTicketAsync(
                employeeToken,
                asset.Id,
                "High filtre talebi",
                TicketPriority.High);

        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Critical filtre talebi",
            TicketPriority.Critical);

        var result =
            await GetPagedTicketsAsync(
                adminToken,
                $"/api/tickets?assetId={asset.Id}" +
                "&status=1&priority=3" +
                "&pageNumber=1&pageSize=20");

        var filteredTicket =
            Assert.Single(result.Items);

        Assert.Equal(
            highTicket.Id,
            filteredTicket.Id);

        Assert.Equal(
            "Open",
            filteredTicket.Status);

        Assert.Equal(
            "High",
            filteredTicket.Priority);
    }

    [Fact]
    public async Task GetTickets_WithEmployeeToken_ReturnsOnlyOwnTickets()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var ownTicket =
            await CreateTicketAsync(
                employeeToken,
                asset.Id,
                "Çalışanın kendi talebi",
                TicketPriority.High);

        var secondEmployee =
            await CreateEmployeeAsync(adminToken);

        var secondEmployeeToken =
            await LoginAsync(
                secondEmployee.Email,
                secondEmployee.Password);

        await CreateTicketAsync(
            secondEmployeeToken,
            asset.Id,
            "Başka çalışanın talebi",
            TicketPriority.Medium);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets?assetId={asset.Id}" +
                "&pageNumber=1&pageSize=20",
                employeeToken);

        var response =
            await _client.SendAsync(request);

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<TicketDto>>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(result);

        var visibleTicket =
            Assert.Single(result.Items);

        Assert.Equal(
            ownTicket.Id,
            visibleTicket.Id);
    }

    [Fact]
    public async Task CreateTicket_WithEmployeeToken_ReturnsCreatedOpenTicket()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var requestBody =
            CreateTicketRequestFor(asset.Id);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(ticket);
        Assert.Equal("Open", ticket.Status);
        Assert.Equal("High", ticket.Priority);
        Assert.Equal(asset.Id, ticket.AssetId);

        Assert.Equal(
            "Test Çalışanı",
            ticket.CreatedByFullName);

        Assert.Null(ticket.AssignedTechnicianId);
    }

    [Fact]
    public async Task GetTicket_WithCreatorToken_ReturnsOk()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.EmployeeToken);

        var response =
            await _client.SendAsync(request);

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(ticket);

        Assert.Equal(
            setup.Ticket.Id,
            ticket.Id);

        Assert.Equal(
            "Test Çalışanı",
            ticket.CreatedByFullName);
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
    public async Task GetTicket_WithAnotherEmployeeToken_ReturnsForbidden()
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
                $"/api/tickets/{setup.Ticket.Id}",
                secondEmployeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);

        var responseText =
            await response.Content
                .ReadAsStringAsync();

        Assert.Contains(
            "Başka bir kullanıcıya ait talebi",
            responseText);
    }

    [Fact]
    public async Task GetTicket_WithAdminToken_ReturnsOk()
    {
        var setup =
            await CreateTicketSetupAsync();

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                setup.AdminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
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
    public async Task CreateTicket_WithMissingAsset_ReturnsNotFound()
    {
        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var requestBody =
            CreateTicketRequestFor(
                Guid.NewGuid());

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithUndefinedStatus_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/tickets?pageNumber=1&pageSize=10&status=999",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
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


    [Fact]
    public async Task CreateTicket_WithInactiveAsset_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        using var statusRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/assets/{asset.Id}/status",
                adminToken,
                new ChangeAssetStatusRequest
                {
                    IsActive = false
                });

        var statusResponse =
            await _client.SendAsync(statusRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            statusResponse.StatusCode);

        using var createRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                CreateTicketRequestFor(asset.Id));

        var createResponse =
            await _client.SendAsync(createRequest);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            createResponse.StatusCode);
    }


    private async Task<TicketSetup> CreateResolvedTicketSetupAsync()
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

        using var resolveRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/resolve",
                technicianToken,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Integration test çözümü."
                });

        var resolveResponse =
            await _client.SendAsync(resolveRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            resolveResponse.StatusCode);

        return setup;
    }

    private async Task<CreatedTechnician> CreateTechnicianAsync(
    string adminToken)
    {
        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var email =
            $"ticket-technician-{Guid.NewGuid():N}@example.com";

        const string password =
            "TechnicianTest123!";

        var requestBody =
            new CreateUserRequest
            {
                FullName = "Test Teknik Personeli",
                Email = email,
                Password = password,
                Role = UserRole.Technician,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var user =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.NotNull(user);

        return new CreatedTechnician(
            user.Id,
            email,
            password);
    }

    private async Task<TicketSetup> CreateClosedTicketSetupAsync()
    {
        var setup =
            await CreateResolvedTicketSetupAsync();

        using var closeRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/tickets/{setup.Ticket.Id}/close",
                setup.EmployeeToken);

        var closeResponse =
            await _client.SendAsync(closeRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            closeResponse.StatusCode);

        return setup;
    }

    private async Task<TicketDto> CreateTicketAsync(
    string accessToken,
    Guid assetId,
    string title,
    TicketPriority priority)
    {
        var requestBody =
            new CreateTicketRequest
            {
                AssetId = assetId,
                Title = title,
                Description =
                    $"{title} için integration test açıklaması.",
                Priority = priority
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                accessToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        return ticket;
    }

    private async Task<PagedResult<TicketDto>>
        GetPagedTicketsAsync(
            string accessToken,
            string requestUri)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                requestUri,
                accessToken);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<TicketDto>>();

        Assert.NotNull(result);

        return result;
    }

    private async Task<TicketSetup> CreateTicketSetupAsync()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var asset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/tickets",
                employeeToken,
                CreateTicketRequestFor(asset.Id));

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var ticket =
            await response.Content
                .ReadFromJsonAsync<TicketDto>();

        Assert.NotNull(ticket);

        return new TicketSetup(
            adminToken,
            employeeToken,
            ticket);
    }

    private async Task<AssetDto> CreateAssetAsync(
        string adminToken,
        Guid departmentId)
    {
        var requestBody =
            new CreateAssetRequest
            {
                Name = "Ticket Test Cihazı",
                SerialNumber =
                    $"TICKET-ASSET-{Guid.NewGuid():N}",
                Type = AssetType.Computer,
                DepartmentId = departmentId,
                Location = "Test Odası"
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var asset =
            await response.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.NotNull(asset);

        return asset;
    }

    private async Task<CreatedEmployee> CreateEmployeeAsync(
        string adminToken)
    {
        var departmentId =
            await GetActiveDepartmentIdAsync(
                adminToken);

        var email =
            $"ticket-user-{Guid.NewGuid():N}@example.com";

        const string password =
            "UserTest123!";

        var requestBody =
            new CreateUserRequest
            {
                FullName = "İkinci Test Çalışanı",
                Email = email,
                Password = password,
                Role = UserRole.Employee,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var user =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.NotNull(user);

        return new CreatedEmployee(
            user.Id,
            email,
            password);
    }

    private async Task<Guid> GetActiveDepartmentIdAsync(
        string accessToken)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/departments",
                accessToken);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var departments =
            await response.Content
                .ReadFromJsonAsync<List<DepartmentDto>>();

        Assert.NotNull(departments);

        var activeDepartment =
            departments.FirstOrDefault(
                department => department.IsActive);

        Assert.NotNull(activeDepartment);

        return activeDepartment.Id;
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var loginRequest =
            new LoginRequest
            {
                Email = email,
                Password = password
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);

        return result.AccessToken;
    }

    private static CreateTicketRequest CreateTicketRequestFor(
        Guid assetId)
    {
        return new CreateTicketRequest
        {
            AssetId = assetId,
            Title = "Bilgisayar açılmıyor",
            Description =
                "Güç düğmesine basıldığında cihaz açılmıyor.",
            Priority = TicketPriority.High
        };
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? content = null)
    {
        var request =
            new HttpRequestMessage(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        if (content is not null)
        {
            request.Content =
                JsonContent.Create(content);
        }

        return request;
    }



    private sealed record CreatedTechnician(
    Guid Id,
    string Email,
    string Password);

    private string CreateTokenWithRole(
    string role)
    {
        var options =
            _factory.Services
                .GetRequiredService<IOptions<JwtOptions>>()
                .Value;

        var signingKey =
            new SymmetricSecurityKey(
                Convert.FromBase64String(
                    options.SigningKey));

        var credentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms.HmacSha256);

        var now = DateTime.UtcNow;

        var token =
            new JwtSecurityToken(
                issuer: options.Issuer,
                audience: options.Audience,
                claims:
                [
                    new Claim(
                    JwtRegisteredClaimNames.Sub,
                    Guid.NewGuid().ToString()),

                new Claim(
                    JwtRegisteredClaimNames.Name,
                    "Desteklenmeyen Rol Kullanıcısı"),

                new Claim(
                    JwtRegisteredClaimNames.Email,
                    "unsupported-role@example.com"),

                new Claim(
                    "role",
                    role),

                new Claim(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString())
                ],
                notBefore: now,
                expires: now.AddMinutes(5),
                signingCredentials: credentials);

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }

    private sealed record TicketSetup(
        string AdminToken,
        string EmployeeToken,
        TicketDto Ticket);

    private sealed record CreatedEmployee(
        Guid Id,
        string Email,
        string Password);
}
