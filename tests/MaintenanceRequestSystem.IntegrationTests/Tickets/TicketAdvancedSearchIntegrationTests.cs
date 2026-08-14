using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Categories.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using System.Net.Http.Json;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task GetTickets_SearchMatchesTitleDescriptionAndTicketNumber()
    {
        var setup = await CreateTicketSetupAsync();

        var titleResult = await GetPagedTicketsAsync(
            setup.EmployeeToken,
            "/api/tickets?search=BILGISAYAR");
        var descriptionResult = await GetPagedTicketsAsync(
            setup.EmployeeToken,
            "/api/tickets?search=güç%20düğmesine");
        var numberResult = await GetPagedTicketsAsync(
            setup.EmployeeToken,
            $"/api/tickets?search={setup.Ticket.TicketNumber.ToLowerInvariant()}");
        var emptyResult = await GetPagedTicketsAsync(
            setup.EmployeeToken,
            "/api/tickets?search=%20%20%20");

        Assert.Contains(titleResult.Items, ticket => ticket.Id == setup.Ticket.Id);
        Assert.Contains(
            descriptionResult.Items,
            ticket => ticket.Id == setup.Ticket.Id);
        Assert.Contains(numberResult.Items, ticket => ticket.Id == setup.Ticket.Id);
        Assert.Contains(emptyResult.Items, ticket => ticket.Id == setup.Ticket.Id);
    }

    [Fact]
    public async Task GetTickets_CategoryFilterIncludesHistoricalInactiveCategory()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);
        var departmentId = await GetActiveDepartmentIdAsync(adminToken);
        var asset = await CreateAssetAsync(adminToken, departmentId);
        var category = await CreateSearchCategoryAsync(adminToken);
        var ticket = await CreateCategorizedTicketAsync(
            employeeToken,
            asset.Id,
            category.Id,
            "Kategori filtre talebi");

        using var statusRequest = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/categories/{category.Id}/status",
            adminToken,
            new ChangeTicketCategoryStatusRequest { IsActive = false });
        (await _client.SendAsync(statusRequest)).EnsureSuccessStatusCode();

        var result = await GetPagedTicketsAsync(
            adminToken,
            $"/api/tickets?categoryId={category.Id}");

        Assert.Contains(
            result.Items,
            item => item.Id == ticket.Id &&
                item.CategoryName == category.Name);
    }

    [Fact]
    public async Task GetTickets_UserDepartmentAndDateFiltersUseAndSemantics()
    {
        var setup = await CreateTicketSetupAsync();
        var currentUser = await GetCurrentUserAsync(setup.EmployeeToken);
        var createdFrom = Uri.EscapeDataString(
            setup.Ticket.CreatedAt.AddMinutes(-1).ToString("O"));
        var createdTo = Uri.EscapeDataString(
            setup.Ticket.CreatedAt.AddMinutes(1).ToString("O"));

        var result = await GetPagedTicketsAsync(
            setup.AdminToken,
            "/api/tickets" +
            $"?createdByUserId={setup.Ticket.CreatedByUserId}" +
            $"&departmentId={currentUser.DepartmentId}" +
            $"&createdFrom={createdFrom}" +
            $"&createdTo={createdTo}" +
            $"&status={TicketStatus.Open}" +
            $"&priority={TicketPriority.High}" +
            $"&assetId={setup.Ticket.AssetId}");

        Assert.Contains(result.Items, ticket => ticket.Id == setup.Ticket.Id);

        using var invalidRangeRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/tickets?createdFrom=2026-08-13T00%3A00%3A00Z" +
            "&createdTo=2026-08-12T00%3A00%3A00Z",
            setup.AdminToken);
        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            (await _client.SendAsync(invalidRangeRequest)).StatusCode);

        using var nonUtcRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/tickets?createdFrom=2026-08-13T00%3A00%3A00",
            setup.AdminToken);
        Assert.Equal(
            System.Net.HttpStatusCode.BadRequest,
            (await _client.SendAsync(nonUtcRequest)).StatusCode);
    }

    [Fact]
    public async Task GetTickets_AssignedTechnicianFilterCannotEscapeTechnicianScope()
    {
        var setup = await CreateTicketSetupAsync();
        var assignedTechnician = await CreateTechnicianAsync(setup.AdminToken);
        var otherTechnician = await CreateTechnicianAsync(setup.AdminToken);
        await AssignTicketAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            assignedTechnician.Id);
        var assignedToken = await LoginAsync(
            assignedTechnician.Email,
            assignedTechnician.Password);

        var adminResult = await GetPagedTicketsAsync(
            setup.AdminToken,
            $"/api/tickets?assignedTechnicianId={assignedTechnician.Id}");
        var scopedNoMatch = await GetPagedTicketsAsync(
            assignedToken,
            $"/api/tickets?assignedTechnicianId={otherTechnician.Id}");

        Assert.Contains(adminResult.Items, ticket => ticket.Id == setup.Ticket.Id);
        Assert.Empty(scopedNoMatch.Items);
    }

    [Fact]
    public async Task GetTickets_CreatedByFilterCannotEscapeEmployeeScope()
    {
        var setup = await CreateTicketSetupAsync();
        var otherEmployee = await CreateEmployeeAsync(setup.AdminToken);

        var result = await GetPagedTicketsAsync(
            setup.EmployeeToken,
            $"/api/tickets?createdByUserId={otherEmployee.Id}");

        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task GetTickets_CategoryFilterIsAppliedBeforePaginationAndSupportsSorting()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var employeeToken = await LoginAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);
        var departmentId = await GetActiveDepartmentIdAsync(adminToken);
        var asset = await CreateAssetAsync(adminToken, departmentId);
        var category = await CreateSearchCategoryAsync(adminToken);

        await CreateCategorizedTicketAsync(
            employeeToken,
            asset.Id,
            category.Id,
            "Zulu kategori talebi");
        await CreateCategorizedTicketAsync(
            employeeToken,
            asset.Id,
            category.Id,
            "Alfa kategori talebi");
        await CreateTicketAsync(
            employeeToken,
            asset.Id,
            "Başka kategori talebi",
            TicketPriority.Low);

        var result = await GetPagedTicketsAsync(
            adminToken,
            $"/api/tickets?categoryId={category.Id}" +
            "&pageNumber=1&pageSize=1&sortBy=title&sortDescending=false");

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal("Alfa kategori talebi", result.Items[0].Title);
    }

    private async Task<TicketCategoryDto> CreateSearchCategoryAsync(
        string adminToken)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/categories",
            adminToken,
            new CreateTicketCategoryRequest
            {
                Name = $"Arama {Guid.NewGuid():N}"
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var category = await response.Content
            .ReadFromJsonAsync<TicketCategoryDto>();
        Assert.NotNull(category);
        return category;
    }

    private async Task<TicketDto> CreateCategorizedTicketAsync(
        string employeeToken,
        Guid assetId,
        Guid categoryId,
        string title)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/tickets",
            employeeToken,
            new CreateTicketRequest
            {
                AssetId = assetId,
                CategoryId = categoryId,
                Title = title,
                Description = $"{title} özel arama açıklaması.",
                Priority = TicketPriority.High
            });
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var ticket = await response.Content.ReadFromJsonAsync<TicketDto>();
        Assert.NotNull(ticket);
        return ticket;
    }

    private async Task<CurrentUserDto> GetCurrentUserAsync(string token)
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/auth/me",
            token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<CurrentUserDto>();
        Assert.NotNull(user);
        return user;
    }
}
