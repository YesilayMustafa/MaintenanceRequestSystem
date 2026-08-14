using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.TicketActivity.Dtos;
using MaintenanceRequestSystem.Application.TicketActivity.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/tickets/{ticketId:guid}/activity")]
[Authorize(
    Roles =
        nameof(UserRole.Employee) + "," +
        nameof(UserRole.Technician) + "," +
        nameof(UserRole.Admin))]
public sealed class TicketActivityController : ControllerBase
{
    private readonly ITicketActivityService _activityService;

    public TicketActivityController(ITicketActivityService activityService)
    {
        _activityService = activityService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResult<TicketActivityDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResult<TicketActivityDto>>> GetAll(
        Guid ticketId,
        [FromQuery] TicketActivityQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await _activityService.GetPagedAsync(
            ticketId,
            userId,
            role,
            query,
            cancellationToken));
    }

    private bool TryGetCurrentUser(out Guid userId, out UserRole role)
    {
        var validUserId = Guid.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out userId);
        var validRole = Enum.TryParse(
                User.FindFirstValue("role"),
                ignoreCase: true,
                out role) &&
            Enum.IsDefined(role);

        return validUserId && validRole;
    }
}
