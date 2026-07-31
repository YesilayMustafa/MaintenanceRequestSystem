using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Application.TicketComments.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/comments")]
[Authorize]
public sealed class TicketCommentsController
    : ControllerBase
{
    private readonly ITicketCommentService
        _commentService;

    public TicketCommentsController(
        ITicketCommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<TicketCommentDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<
        ActionResult<IReadOnlyList<TicketCommentDto>>> GetAll(
            Guid ticketId,
            CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var comments =
            await _commentService.GetByTicketIdAsync(
                ticketId,
                userId,
                role,
                cancellationToken);

        return Ok(comments);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(TicketCommentDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketCommentDto>> Create(
        Guid ticketId,
        CreateTicketCommentRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var comment =
            await _commentService.CreateAsync(
                ticketId,
                userId,
                role,
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetAll),
            new { ticketId },
            comment);
    }

    private bool TryGetCurrentUser(
        out Guid userId,
        out UserRole role)
    {
        var userIdValue =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        var roleValue =
            User.FindFirstValue("role");

        var validUserId =
            Guid.TryParse(
                userIdValue,
                out userId);

        var validRole =
            Enum.TryParse(
                roleValue,
                ignoreCase: true,
                out role);

        return validUserId && validRole;
    }
}