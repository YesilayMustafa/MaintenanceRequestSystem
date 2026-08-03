namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed class AssignTicketRequest
{
    public Guid TechnicianId { get; init; }
}