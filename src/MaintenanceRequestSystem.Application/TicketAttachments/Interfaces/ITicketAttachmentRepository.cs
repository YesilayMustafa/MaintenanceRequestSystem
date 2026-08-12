using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;

public interface ITicketAttachmentRepository
{
    Task<IReadOnlyList<TicketAttachment>> GetByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<TicketAttachment?> GetByIdAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default);

    Task<int> CountByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TicketAttachment attachment,
        CancellationToken cancellationToken = default);

    void Remove(TicketAttachment attachment);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
