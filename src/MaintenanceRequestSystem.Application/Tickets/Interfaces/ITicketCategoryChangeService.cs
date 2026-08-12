using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketCategoryChangeService
{
    Task<TicketDto> ChangeCategoryAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ChangeTicketCategoryRequest request,
        CancellationToken cancellationToken = default);
}
