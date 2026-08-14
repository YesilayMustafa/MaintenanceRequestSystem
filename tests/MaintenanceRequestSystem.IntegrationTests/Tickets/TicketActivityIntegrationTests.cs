using System.Net;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Categories.Dtos;
using MaintenanceRequestSystem.Application.TicketActivity.Dtos;
using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task Activity_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync(
            $"/api/tickets/{Guid.NewGuid()}/activity");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Activity_EmployeeOwnTicket_ReturnsCreatedEvent()
    {
        var setup = await CreateTicketSetupAsync();

        var result = await GetActivityAsync(
            setup.Ticket.Id,
            setup.EmployeeToken);

        var created = Assert.Single(result.Items);
        Assert.Equal("TicketCreated", created.Type);
        Assert.Equal(setup.Ticket.CreatedByUserId, created.ActorUserId);
    }

    [Fact]
    public async Task Activity_Admin_ReturnsSupportedEventsWithoutSensitiveFields()
    {
        var setup = await CreateTicketSetupAsync();
        var technician = await CreateTechnicianAsync(setup.AdminToken);
        await AssignTicketAsync(setup.AdminToken, setup.Ticket.Id, technician.Id);
        var technicianToken = await LoginAsync(
            technician.Email,
            technician.Password);
        using var progressRequest = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{setup.Ticket.Id}/start-progress",
            technicianToken);
        var progressResponse = await _client.SendAsync(progressRequest);
        Assert.Equal(HttpStatusCode.OK, progressResponse.StatusCode);
        using var createCategoryRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            "/api/categories",
            setup.AdminToken,
            new CreateTicketCategoryRequest
            {
                Name = $"Timeline {Guid.NewGuid():N}"
            });
        var createCategoryResponse = await _client.SendAsync(createCategoryRequest);
        Assert.Equal(HttpStatusCode.Created, createCategoryResponse.StatusCode);
        var category = await createCategoryResponse.Content
            .ReadFromJsonAsync<TicketCategoryDto>();
        Assert.NotNull(category);
        using var categoryRequest = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{setup.Ticket.Id}/category",
            setup.AdminToken,
            new ChangeTicketCategoryRequest { CategoryId = category.Id });
        var categoryResponse = await _client.SendAsync(categoryRequest);
        Assert.Equal(HttpStatusCode.OK, categoryResponse.StatusCode);
        using var commentRequest = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/tickets/{setup.Ticket.Id}/comments",
            setup.EmployeeToken,
            new CreateTicketCommentRequest { Content = "Timeline yorumu" });
        var commentResponse = await _client.SendAsync(commentRequest);
        Assert.Equal(HttpStatusCode.Created, commentResponse.StatusCode);
        var (attachmentResponse, _) = await UploadAttachmentAsync(
            setup.EmployeeToken,
            setup.Ticket.Id,
            "timeline.pdf",
            "application/pdf",
            [1, 2, 3]);
        Assert.Equal(HttpStatusCode.Created, attachmentResponse.StatusCode);
        using var priorityRequest = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{setup.Ticket.Id}/priority",
            setup.AdminToken,
            new ChangeTicketPriorityRequest { Priority = TicketPriority.Critical });
        var priorityResponse = await _client.SendAsync(priorityRequest);
        Assert.Equal(HttpStatusCode.OK, priorityResponse.StatusCode);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/activity?pageSize=100",
            setup.AdminToken);
        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("storageKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("oldValues", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("newValues", body, StringComparison.OrdinalIgnoreCase);
        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<TicketActivityDto>>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, item => item.Type == "TicketCreated");
        Assert.Contains(result.Items, item => item.Type == "AssignmentChanged");
        Assert.Contains(result.Items, item => item.Type == "StatusChanged");
        Assert.Contains(result.Items, item => item.Type == "CategoryChanged");
        Assert.Contains(result.Items, item => item.Type == "CommentAdded");
        Assert.Contains(result.Items, item => item.Type == "AttachmentUploaded");
        Assert.Contains(result.Items, item => item.Type == "PriorityChanged");
        Assert.True(result.Items
            .Zip(result.Items.Skip(1))
            .All(pair => pair.First.CreatedAt >= pair.Second.CreatedAt));
    }

    [Fact]
    public async Task Activity_AssignedTechnician_ReturnsOk()
    {
        var setup = await CreateTicketSetupAsync();
        var technician = await CreateTechnicianAsync(setup.AdminToken);
        await AssignTicketAsync(setup.AdminToken, setup.Ticket.Id, technician.Id);
        var token = await LoginAsync(technician.Email, technician.Password);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/activity",
            token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Activity_DifferentEmployee_ReturnsForbidden()
    {
        var setup = await CreateTicketSetupAsync();
        var otherEmployee = await CreateEmployeeAsync(setup.AdminToken);
        var token = await LoginAsync(otherEmployee.Email, otherEmployee.Password);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/activity",
            token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Activity_UnassignedTechnician_ReturnsForbidden()
    {
        var setup = await CreateTicketSetupAsync();
        var technician = await CreateTechnicianAsync(setup.AdminToken);
        var token = await LoginAsync(technician.Email, technician.Password);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/activity",
            token);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Activity_Pagination_ReturnsDistinctPages()
    {
        var setup = await CreateTicketSetupAsync();
        using var firstComment = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/tickets/{setup.Ticket.Id}/comments",
            setup.EmployeeToken,
            new CreateTicketCommentRequest { Content = "İlk yorum" });
        using var secondComment = CreateAuthorizedRequest(
            HttpMethod.Post,
            $"/api/tickets/{setup.Ticket.Id}/comments",
            setup.EmployeeToken,
            new CreateTicketCommentRequest { Content = "İkinci yorum" });
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(firstComment)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await _client.SendAsync(secondComment)).StatusCode);

        var firstPage = await GetActivityAsync(
            setup.Ticket.Id,
            setup.EmployeeToken,
            "?pageNumber=1&pageSize=2");
        var secondPage = await GetActivityAsync(
            setup.Ticket.Id,
            setup.EmployeeToken,
            "?pageNumber=2&pageSize=2");

        Assert.Equal(3, firstPage.TotalCount);
        Assert.Equal(2, firstPage.TotalPages);
        Assert.Equal(2, firstPage.Items.Count);
        Assert.Single(secondPage.Items);
        Assert.Empty(firstPage.Items.Select(item => item.Id)
            .Intersect(secondPage.Items.Select(item => item.Id)));
    }

    private async Task<PagedResult<TicketActivityDto>> GetActivityAsync(
        Guid ticketId,
        string token,
        string query = "")
    {
        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{ticketId}/activity{query}",
            token);
        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var result = await response.Content
            .ReadFromJsonAsync<PagedResult<TicketActivityDto>>();
        Assert.NotNull(result);
        return result;
    }
}
