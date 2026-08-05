namespace MaintenanceRequestSystem.Application.AuditLogs.Dtos;

/// <summary>
/// Bir audit kaydının API üzerinden döndürülen
/// görüntülenebilir modelidir.
/// </summary>
public sealed class AuditLogDto
{
    /// <summary>
    /// Audit kaydının benzersiz kimliğidir.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// İşlemi gerçekleştiren kullanıcının kimliğidir.
    /// </summary>
    public Guid PerformedByUserId { get; init; }

    /// <summary>
    /// İşlemi gerçekleştiren kullanıcının adıdır.
    /// </summary>
    public string PerformedByUserFullName { get; init; }
        = string.Empty;

    /// <summary>
    /// Gerçekleştirilen işlemin adıdır.
    /// </summary>
    public string Action { get; init; }
        = string.Empty;

    /// <summary>
    /// İşlem yapılan entity türüdür.
    /// </summary>
    public string EntityName { get; init; }
        = string.Empty;

    /// <summary>
    /// İşlem yapılan entity kimliğidir.
    /// </summary>
    public string EntityId { get; init; }
        = string.Empty;

    /// <summary>
    /// İşlem öncesindeki değerlerin JSON karşılığıdır.
    /// </summary>
    public string? OldValues { get; init; }

    /// <summary>
    /// İşlem sonrasındaki değerlerin JSON karşılığıdır.
    /// </summary>
    public string? NewValues { get; init; }

    /// <summary>
    /// Audit kaydının UTC oluşturulma zamanıdır.
    /// </summary>
    public DateTime CreatedAt { get; init; }
}
