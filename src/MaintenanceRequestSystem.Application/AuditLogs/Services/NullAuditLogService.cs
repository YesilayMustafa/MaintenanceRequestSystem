using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Application.AuditLogs.Services;

/// <summary>
/// Audit kaydı üretmek gerekmeyen test ve yardımcı kullanımlar için
/// hiçbir işlem yapmayan audit servisidir.
/// </summary>
public sealed class NullAuditLogService
    : IAuditLogService
{

    public Task<PagedResult<AuditLogDto>> GetPagedAsync(
    AuditLogListQuery query,
    CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new PagedResult<AuditLogDto>(
                Array.Empty<AuditLogDto>(),
                query.PageNumber,
                query.PageSize,
                0,
                0));
    }
    public Task AddAsync(
        Guid performedByUserId,
        string action,
        string entityName,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
