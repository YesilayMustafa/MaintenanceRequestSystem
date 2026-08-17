using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

internal static class TicketQueryScope
{
    internal static readonly TicketStatus[] ActiveStatuses =
    [
        TicketStatus.Open,
        TicketStatus.Assigned,
        TicketStatus.InProgress,
        TicketStatus.Waiting,
        TicketStatus.Resolved
    ];

    internal static IQueryable<Ticket> Apply(
        IQueryable<Ticket> query,
        Guid currentUserId,
        UserRole currentUserRole)
    {
        return currentUserRole switch
        {
            UserRole.Employee =>
                query.Where(ticket =>
                    ticket.CreatedByUserId == currentUserId),

            UserRole.Technician =>
                query.Where(ticket =>
                    ticket.AssignedTechnicianId == currentUserId),

            UserRole.Admin => query,

            _ => query.Where(_ => false)
        };
    }
}
