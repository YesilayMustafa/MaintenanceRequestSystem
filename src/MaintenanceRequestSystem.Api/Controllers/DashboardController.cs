using MaintenanceRequestSystem.Api.Authentication;
using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Application.Dashboard.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(
    Roles =
        nameof(UserRole.Employee) + "," +
        nameof(UserRole.Technician) + "," +
        nameof(UserRole.Admin))]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public DashboardController(
        IDashboardService dashboardService,
        ICurrentUserAccessor currentUserAccessor)
    {
        _dashboardService = dashboardService;
        _currentUserAccessor = currentUserAccessor;
    }

    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardDto>> Get(
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(
                out var currentUserId,
                out var currentUserRole))
        {
            return Unauthorized();
        }

        var dashboard =
            await _dashboardService.GetAsync(
                currentUserId,
                currentUserRole,
                cancellationToken);

        return Ok(dashboard);
    }
}
