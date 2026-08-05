using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

/// <summary>
/// Talebe atanacak yeni öncelik bilgisini taşır.
/// </summary>
public sealed class ChangeTicketPriorityRequest
{
    public TicketPriority Priority { get; init; }
}
