using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Application.AuditLogs.Interfaces;

/// <summary>
/// Kritik sistem işlemleri için audit kaydı oluşturur.
/// </summary>
public interface IAuditLogService
{

    /// <summary>
    /// Audit kayıtlarını filtrelenmiş ve sayfalanmış olarak getirir.
    /// </summary>
    Task<PagedResult<AuditLogDto>> GetPagedAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Audit kaydını mevcut çalışma birimine ekler.
    /// Kalıcı kayıt, çağıran servisin SaveChanges işlemiyle yapılır.
    /// </summary>
    Task AddAsync(
        Guid performedByUserId,
        string action,
        string entityName,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default);
}
