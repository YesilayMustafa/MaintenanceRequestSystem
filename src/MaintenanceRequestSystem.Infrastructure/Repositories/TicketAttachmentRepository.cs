using MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class TicketAttachmentRepository
    : ITicketAttachmentRepository
{
    private readonly ApplicationDbContext _context;

    public TicketAttachmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TicketAttachment>> GetByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _context.TicketAttachments
            .AsNoTracking()
            .Include(attachment => attachment.UploadedByUser)
            .Where(attachment => attachment.TicketId == ticketId)
            .OrderBy(attachment => attachment.CreatedAt)
            .ThenBy(attachment => attachment.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<TicketAttachment?> GetByIdAsync(
        Guid ticketId,
        Guid attachmentId,
        CancellationToken cancellationToken = default)
    {
        return _context.TicketAttachments
            .Include(attachment => attachment.UploadedByUser)
            .FirstOrDefaultAsync(
                attachment =>
                    attachment.Id == attachmentId &&
                    attachment.TicketId == ticketId,
                cancellationToken);
    }

    public Task<int> CountByTicketIdAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return _context.TicketAttachments.CountAsync(
            attachment => attachment.TicketId == ticketId,
            cancellationToken);
    }

    public Task AddAsync(
        TicketAttachment attachment,
        CancellationToken cancellationToken = default)
    {
        return _context.TicketAttachments.AddAsync(
            attachment,
            cancellationToken).AsTask();
    }

    public void Remove(TicketAttachment attachment)
    {
        _context.TicketAttachments.Remove(attachment);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}
