namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

/// <summary>
/// Kapatılmış talebin neden yeniden açıldığını taşır.
/// </summary>
public sealed class ReopenTicketRequest
{
    public string Reason { get; init; } =
        string.Empty;
}
