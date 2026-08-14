using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.TicketActivity.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.TicketActivity.Interfaces;

public interface ITicketActivityService
{
    Task<PagedResult<TicketActivityDto>> GetPagedAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        TicketActivityQuery query,
        CancellationToken cancellationToken = default);
}
