using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketAssignmentService
{
    Task<TicketDto> AssignAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketDto> ReassignAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default);
}
