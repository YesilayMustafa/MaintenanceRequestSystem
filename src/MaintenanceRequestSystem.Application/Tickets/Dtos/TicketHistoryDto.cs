namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

/// <summary>
/// Talep üzerinde gerçekleşen bir durum değişikliğini temsil eder.
/// </summary>
public sealed class TicketHistoryDto
{
    public Guid Id { get; init; }

    public Guid PerformedByUserId { get; init; }

    public string? OldStatus { get; init; }

    public string NewStatus { get; init; } =
        string.Empty;

    public string Description { get; init; } =
        string.Empty;

    public DateTime OccurredAt { get; init; }
}
