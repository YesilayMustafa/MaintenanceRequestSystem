namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

/// <summary>
/// Bir talebin neden beklemeye alındığını taşır.
/// </summary>
public sealed class PutTicketOnHoldRequest
{
    public string Reason { get; init; } = string.Empty;
}
