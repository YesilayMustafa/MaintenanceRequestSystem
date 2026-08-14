using MaintenanceRequestSystem.Application.TicketActivity.Dtos;

namespace MaintenanceRequestSystem.Application.TicketActivity.Interfaces;

public interface ITicketActivityRepository
{
    Task<(IReadOnlyList<TicketActivityDto> Items, int TotalCount)> GetPagedAsync(
        Guid ticketId,
        TicketActivityQuery query,
        CancellationToken cancellationToken = default);
}
