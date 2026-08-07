using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketTechnicianLifecycleService
{
    Task<TicketDto> StartProgressAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<TicketDto> PutOnHoldAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        PutTicketOnHoldRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketDto> ResumeAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<TicketDto> ResolveAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ResolveTicketRequest request,
        CancellationToken cancellationToken = default);
}
