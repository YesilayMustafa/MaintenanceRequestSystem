using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.TicketAttachments.Dtos;
using MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;
using MaintenanceRequestSystem.Application.TicketAttachments.Models;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/attachments")]
[Authorize(
    Roles =
        nameof(UserRole.Employee) + "," +
        nameof(UserRole.Technician) + "," +
        nameof(UserRole.Admin))]
public sealed class TicketAttachmentsController : ControllerBase
{
    private readonly ITicketAttachmentService _attachmentService;

    public TicketAttachmentsController(
        ITicketAttachmentService attachmentService)
    {
        _attachmentService = attachmentService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<TicketAttachmentDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TicketAttachmentDto>>> GetAll(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await _attachmentService.GetAllAsync(
            ticketId,
            userId,
            role,
            cancellationToken));
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(
        typeof(TicketAttachmentDto),
        StatusCodes.Status201Created)]
    public async Task<ActionResult<TicketAttachmentDto>> Upload(
        Guid ticketId,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        if (file is null)
        {
            return BadRequest();
        }

        await using var content = file.OpenReadStream();

        var attachment = await _attachmentService.UploadAsync(
            ticketId,
            userId,
            role,
            new AttachmentUpload(
                content,
                file.FileName,
                file.ContentType,
                file.Length),
            cancellationToken);

        return CreatedAtAction(
            nameof(Download),
            new
            {
                ticketId,
                attachmentId = attachment.Id
            },
            attachment);
    }

    [HttpGet("{attachmentId:guid}/download")]
    public async Task<IActionResult> Download(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        var download = await _attachmentService.DownloadAsync(
            ticketId,
            attachmentId,
            userId,
            role,
            cancellationToken);

        Response.Headers["X-Content-Type-Options"] = "nosniff";

        return File(
            download.Content,
            download.ContentType,
            download.OriginalFileName);
    }

    [HttpDelete("{attachmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        await _attachmentService.DeleteAsync(
            ticketId,
            attachmentId,
            userId,
            role,
            cancellationToken);

        return NoContent();
    }

    private bool TryGetCurrentUser(
        out Guid userId,
        out UserRole role)
    {
        var userIdValue = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var roleValue = User.FindFirstValue("role");

        var validUserId = Guid.TryParse(userIdValue, out userId);
        var validRole = Enum.TryParse<UserRole>(
                roleValue,
                ignoreCase: true,
                out role) &&
            Enum.IsDefined(role);

        return validUserId && validRole;
    }
}
