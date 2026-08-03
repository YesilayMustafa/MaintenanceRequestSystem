using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.TicketComments.Interfaces;

public interface ITicketCommentService
{
    Task<IReadOnlyList<TicketCommentDto>> GetByTicketIdAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<TicketCommentDto> CreateAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        CreateTicketCommentRequest request,
        CancellationToken cancellationToken = default);
}