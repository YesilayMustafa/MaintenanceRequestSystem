using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Notifications.Dtos;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResult<NotificationDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<NotificationDto>>> GetAll(
        [FromQuery] NotificationListQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _notificationService.GetPagedAsync(
            userId,
            query,
            cancellationToken));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(
        typeof(UnreadNotificationCountDto),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadNotificationCountDto>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        return Ok(await _notificationService.GetUnreadCountAsync(
            userId,
            cancellationToken));
    }

    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAsReadAsync(
            userId,
            id,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return Unauthorized();
        }

        await _notificationService.MarkAllAsReadAsync(
            userId,
            cancellationToken);

        return NoContent();
    }

    private bool TryGetCurrentUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out userId);
    }
}
