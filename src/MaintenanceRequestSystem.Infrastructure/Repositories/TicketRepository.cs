using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Ticket?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(ticket => ticket.Asset)
            .Include(ticket => ticket.CreatedByUser)
            .Include(ticket => ticket.AssignedTechnician)
            .FirstOrDefaultAsync(
                ticket => ticket.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        await _context.Tickets.AddAsync(
            ticket,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}