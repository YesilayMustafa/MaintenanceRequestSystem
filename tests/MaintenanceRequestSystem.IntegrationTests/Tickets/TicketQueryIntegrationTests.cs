using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using System.Net;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
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
    public async Task GetTickets_WithUnsupportedRoleToken_ReturnsUnauthorized()
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
            HttpStatusCode.Unauthorized,
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
    public async Task GetTickets_WithTechnicianToken_ReturnsOnlyAssignedTickets()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var otherTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var otherTechnicianTicket =
            await CreateTicketAsync(
                setup.EmployeeToken,
                setup.Ticket.AssetId,
                "Başka teknisyenin talebi",
                TicketPriority.Medium);

        var unassignedTicket =
            await CreateTicketAsync(
                setup.EmployeeToken,
                setup.Ticket.AssetId,
                "Atanmamış talep",
                TicketPriority.Low);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            assignedTechnician.Id);

        await AssignTicketAsync(
            setup.AdminToken,
            otherTechnicianTicket.Id,
            otherTechnician.Id);

        var technicianToken =
            await LoginAsync(
                assignedTechnician.Email,
                assignedTechnician.Password);

        var result =
            await GetPagedTicketsAsync(
                technicianToken,
                $"/api/tickets?assetId={setup.Ticket.AssetId}" +
                "&pageNumber=1&pageSize=20");

        var visibleTicket =
            Assert.Single(result.Items);

        Assert.Equal(
            setup.Ticket.Id,
            visibleTicket.Id);

        Assert.DoesNotContain(
            result.Items,
            ticket => ticket.Id == otherTechnicianTicket.Id);

        Assert.DoesNotContain(
            result.Items,
            ticket => ticket.Id == unassignedTicket.Id);
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
    public async Task GetTicket_WithAssignedTechnicianToken_ReturnsOk()
    {
        var setup =
            await CreateTicketSetupAsync();

        var technician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            technician.Id);

        var technicianToken =
            await LoginAsync(
                technician.Email,
                technician.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                technicianToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task GetTicket_WithDifferentTechnicianToken_ReturnsForbidden()
    {
        var setup =
            await CreateTicketSetupAsync();

        var assignedTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        var differentTechnician =
            await CreateTechnicianAsync(
                setup.AdminToken);

        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            assignedTechnician.Id);

        var technicianToken =
            await LoginAsync(
                differentTechnician.Email,
                differentTechnician.Password);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/tickets/{setup.Ticket.Id}",
                technicianToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
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

}
