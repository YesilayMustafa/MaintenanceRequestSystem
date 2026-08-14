using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.Reports.Dtos;
using MaintenanceRequestSystem.Application.Reports.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("overview")]
    [ProducesResponseType(typeof(ReportOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ReportOverviewDto>> GetOverview(
        [FromQuery] ReportFilterQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRole(out var role))
        {
            return Unauthorized();
        }

        return Ok(await _reportService.GetOverviewAsync(
            role,
            query,
            cancellationToken));
    }

    [HttpGet("tickets/export.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportTickets(
        [FromQuery] ReportFilterQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentRole(out var role))
        {
            return Unauthorized();
        }

        var file = await _reportService.ExportTicketsAsync(
            role,
            query,
            cancellationToken);

        return File(file.Content, file.ContentType, file.FileName);
    }

    private bool TryGetCurrentRole(out UserRole role)
    {
        return Enum.TryParse(
                User.FindFirstValue("role"),
                ignoreCase: true,
                out role) &&
            Enum.IsDefined(role) &&
            User.FindFirstValue(JwtRegisteredClaimNames.Sub) is not null;
    }
}
