using MaintenanceRequestSystem.Application.TicketComments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class TicketCommentRepository
    : ITicketCommentRepository
{
    private readonly ApplicationDbContext _context;

    public TicketCommentRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TicketComment>>
        GetByTicketIdAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
    {
        return await _context.TicketComments
            .AsNoTracking()
            .Include(comment => comment.User)
            .Where(comment =>
                comment.TicketId == ticketId)
            .OrderBy(comment => comment.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        TicketComment comment,
        CancellationToken cancellationToken = default)
    {
        await _context.TicketComments.AddAsync(
            comment,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}