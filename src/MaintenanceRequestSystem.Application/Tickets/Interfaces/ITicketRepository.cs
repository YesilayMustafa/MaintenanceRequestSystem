using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}