using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.TicketComments.Interfaces;

public interface ITicketCommentRepository
{
    Task<IReadOnlyList<TicketComment>> GetByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TicketComment comment,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}