namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

/// <summary>
/// Ticket atama veya yeniden atama işleminde hedef teknik personeli belirtir.
/// </summary>
public sealed class AssignTicketRequest
{
    public Guid TechnicianId { get; init; }
}
