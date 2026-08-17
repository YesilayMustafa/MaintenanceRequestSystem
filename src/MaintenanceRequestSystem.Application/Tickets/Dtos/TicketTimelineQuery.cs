using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed class TicketTimelineQuery
{
    public DateTime From { get; init; }

    public DateTime To { get; init; }

    public TicketStatus? Status { get; init; }

    public TicketPriority? Priority { get; init; }

    public SlaStatus? SlaStatus { get; init; }

    public Guid? CategoryId { get; init; }

    public Guid? AssignedTechnicianId { get; init; }

    public Guid? DepartmentId { get; init; }
}
