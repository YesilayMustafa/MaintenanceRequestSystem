using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;


namespace MaintenanceRequestSystem.Application.AuditLogs.Interfaces;

/// <summary>
/// Audit kayıtlarının kalıcı depoya eklenmesini sağlar.
/// </summary>
public interface IAuditLogRepository
{

    /// <summary>
    /// Audit kayıtlarını filtrelenmiş ve sayfalanmış olarak getirir.
    /// </summary>
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Yeni bir audit kaydını takip edilmeye başlar.
    /// </summary>
    Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bekleyen değişiklikleri kalıcı depoya kaydeder.
    /// </summary>
    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
