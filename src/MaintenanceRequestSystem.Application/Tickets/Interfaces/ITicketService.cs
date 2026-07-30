using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketService
{
    Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default);

    Task<TicketDto> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
}