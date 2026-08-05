namespace MaintenanceRequestSystem.Application.AuditLogs.Dtos;

/// <summary>
/// Audit kayıtlarının sayfalanması ve filtrelenmesi için
/// kullanılan sorgu parametrelerini içerir.
/// </summary>
public sealed class AuditLogListQuery
{
    /// <summary>
    /// İstenen sayfa numarasıdır.
    /// </summary>
    public int PageNumber { get; init; } = 1;

    /// <summary>
    /// Bir sayfada döndürülecek kayıt sayısıdır.
    /// </summary>
    public int PageSize { get; init; } = 10;

    /// <summary>
    /// İşlemi gerçekleştiren kullanıcıya göre filtreler.
    /// </summary>
    public Guid? PerformedByUserId { get; init; }

    /// <summary>
    /// Audit işlem adına göre filtreler.
    /// Örnek: TicketAssigned.
    /// </summary>
    public string? Action { get; init; }

    /// <summary>
    /// İşlem yapılan entity türüne göre filtreler.
    /// Örnek: Ticket.
    /// </summary>
    public string? EntityName { get; init; }

    /// <summary>
    /// İşlem yapılan entity kimliğine göre filtreler.
    /// </summary>
    public string? EntityId { get; init; }

    /// <summary>
    /// Başlangıç tarihinden sonraki kayıtları filtreler.
    /// </summary>
    public DateTime? StartDate { get; init; }

    /// <summary>
    /// Bitiş tarihinden önceki kayıtları filtreler.
    /// </summary>
    public DateTime? EndDate { get; init; }
}
