using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketQueryService
{
    Task<PagedResult<TicketDto>> GetPagedAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        TicketListQuery query,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketTimelineItemDto>> GetTimelineAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        TicketTimelineQuery query,
        CancellationToken cancellationToken = default);

    Task<TicketDto> GetByIdAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
}
