using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using MaintenanceRequestSystem.Application.TicketAttachments.Dtos;

namespace MaintenanceRequestSystem.IntegrationTests.Tickets;

public sealed partial class TicketManagementIntegrationTests
{
    [Fact]
    public async Task Attachments_EmployeeOwnTicket_CanUploadListAndDownload()
    {
        var setup = await CreateTicketSetupAsync();
        var content = new byte[] { 1, 2, 3, 4 };

        var (response, responseBody) = await UploadAttachmentAsync(
            setup.EmployeeToken,
            setup.Ticket.Id,
            "evidence.pdf",
            "application/pdf",
            content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.DoesNotContain("storageKey", responseBody, StringComparison.OrdinalIgnoreCase);

        var attachment = JsonSerializer.Deserialize<TicketAttachmentDto>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(attachment);

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/attachments",
            setup.EmployeeToken);
        var listResponse = await _client.SendAsync(listRequest);
        var listBody = await listResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.DoesNotContain("storageKey", listBody, StringComparison.OrdinalIgnoreCase);

        using var downloadRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/attachments/{attachment.Id}/download",
            setup.EmployeeToken);
        var downloadResponse = await _client.SendAsync(downloadRequest);

        Assert.Equal(HttpStatusCode.OK, downloadResponse.StatusCode);
        Assert.Equal(content, await downloadResponse.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", downloadResponse.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("attachment", downloadResponse.Content.Headers.ContentDisposition?.DispositionType);
    }

    [Fact]
    public async Task Attachments_EmployeeDifferentTicket_ReturnsForbidden()
    {
        var setup = await CreateTicketSetupAsync();
        var otherEmployee = await CreateEmployeeAsync(setup.AdminToken);
        var otherToken = await LoginAsync(otherEmployee.Email, otherEmployee.Password);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/attachments",
            otherToken);

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Attachments_AssignedTechnician_CanUploadAndList()
    {
        var setup = await CreateTicketSetupAsync();
        var technician = await CreateTechnicianAsync(setup.AdminToken);
        await AssignTicketAsync(setup.AdminToken, setup.Ticket.Id, technician.Id);
        var technicianToken = await LoginAsync(
            technician.Email,
            technician.Password);

        var (uploadResponse, _) = await UploadAttachmentAsync(
            technicianToken,
            setup.Ticket.Id,
            "photo.png",
            "image/png",
            new byte[] { 10, 20, 30 });

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/attachments",
            technicianToken);
        var listResponse = await _client.SendAsync(listRequest);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
    }

    [Fact]
    public async Task Attachments_Admin_CanAccessAndDeleteEmployeeAttachment()
    {
        var setup = await CreateTicketSetupAsync();
        var (_, responseBody) = await UploadAttachmentAsync(
            setup.EmployeeToken,
            setup.Ticket.Id,
            "admin-check.pdf",
            "application/pdf",
            new byte[] { 1, 2 });
        var attachment = JsonSerializer.Deserialize<TicketAttachmentDto>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(attachment);

        using var listRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            $"/api/tickets/{setup.Ticket.Id}/attachments",
            setup.AdminToken);
        var listResponse = await _client.SendAsync(listRequest);
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        using var deleteRequest = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/tickets/{setup.Ticket.Id}/attachments/{attachment.Id}",
            setup.AdminToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_WithUnsupportedExtension_ReturnsBadRequest()
    {
        var setup = await CreateTicketSetupAsync();
        var (response, _) = await UploadAttachmentAsync(
            setup.EmployeeToken,
            setup.Ticket.Id,
            "script.exe",
            "application/octet-stream",
            new byte[] { 1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_WhenOversized_ReturnsBadRequest()
    {
        var setup = await CreateTicketSetupAsync();
        var content = new byte[(10 * 1024 * 1024) + 1];
        var (response, _) = await UploadAttachmentAsync(
            setup.EmployeeToken,
            setup.Ticket.Id,
            "large.pdf",
            "application/pdf",
            content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAttachment_WhenTicketClosedOrCancelled_ReturnsBadRequest()
    {
        var closedSetup = await CreateClosedTicketSetupAsync();
        var (closedResponse, _) = await UploadAttachmentAsync(
            closedSetup.EmployeeToken,
            closedSetup.Ticket.Id,
            "closed.pdf",
            "application/pdf",
            new byte[] { 1 });

        var cancelledSetup = await CreateTicketSetupAsync();
        using var cancelRequest = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/tickets/{cancelledSetup.Ticket.Id}/cancel",
            cancelledSetup.EmployeeToken);
        var cancelResponse = await _client.SendAsync(cancelRequest);
        Assert.Equal(HttpStatusCode.OK, cancelResponse.StatusCode);

        var (cancelledResponse, _) = await UploadAttachmentAsync(
            cancelledSetup.EmployeeToken,
            cancelledSetup.Ticket.Id,
            "cancelled.pdf",
            "application/pdf",
            new byte[] { 1 });

        Assert.Equal(HttpStatusCode.BadRequest, closedResponse.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, cancelledResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteAttachment_ByUploader_ReturnsNoContent()
    {
        var setup = await CreateTicketSetupAsync();
        var (_, responseBody) = await UploadAttachmentAsync(
            setup.EmployeeToken,
            setup.Ticket.Id,
            "delete-me.pdf",
            "application/pdf",
            new byte[] { 1 });
        var attachment = JsonSerializer.Deserialize<TicketAttachmentDto>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(attachment);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/tickets/{setup.Ticket.Id}/attachments/{attachment.Id}",
            setup.EmployeeToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteAttachment_ByDifferentAccessibleUser_ReturnsForbidden()
    {
        var setup = await CreateTicketSetupAsync();
        var (_, responseBody) = await UploadAttachmentAsync(
            setup.AdminToken,
            setup.Ticket.Id,
            "admin-upload.pdf",
            "application/pdf",
            new byte[] { 1 });
        var attachment = JsonSerializer.Deserialize<TicketAttachmentDto>(
            responseBody,
            new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(attachment);

        using var request = CreateAuthorizedRequest(
            HttpMethod.Delete,
            $"/api/tickets/{setup.Ticket.Id}/attachments/{attachment.Id}",
            setup.EmployeeToken);
        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(HttpResponseMessage Response, string Body)>
        UploadAttachmentAsync(
            string accessToken,
            Guid ticketId,
            string fileName,
            string contentType,
            byte[] content)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/api/tickets/{ticketId}/attachments");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken);

        using var multipart = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        multipart.Add(fileContent, "file", fileName);
        request.Content = multipart;

        var response = await _client.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        return (response, body);
    }
}
