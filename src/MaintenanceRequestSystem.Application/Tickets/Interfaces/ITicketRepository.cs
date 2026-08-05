using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

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
    Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetPagedAsync(
    Guid currentUserId,
    UserRole currentUserRole,
    TicketListQuery query,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Belirtilen talebe ait durum geçmişini kronolojik olarak getirir.
    /// </summary>
    Task<IReadOnlyList<TicketHistory>> GetHistoriesAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);
}
