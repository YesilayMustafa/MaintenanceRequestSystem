using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

/// <summary>
/// Sistem audit kayıtlarını görüntülemek için
/// yönetici işlemlerini içerir.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(
        IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    /// <summary>
    /// Audit kayıtlarını filtrelenmiş ve sayfalanmış
    /// olarak getirir.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(
        typeof(PagedResult<AuditLogDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAll(
        [FromQuery] AuditLogListQuery query,
        CancellationToken cancellationToken)
    {
        var result =
            await _auditLogService.GetPagedAsync(
                query,
                cancellationToken);

        return Ok(result);
    }
}
