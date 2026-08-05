namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

/// <summary>
/// Teknik personelin talep için girdiği çözüm açıklamasını taşır.
/// </summary>
public sealed class ResolveTicketRequest
{
    public string ResolutionDescription { get; init; } =
        string.Empty;
}
