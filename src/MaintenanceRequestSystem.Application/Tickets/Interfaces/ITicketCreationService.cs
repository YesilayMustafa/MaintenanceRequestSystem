using MaintenanceRequestSystem.Application.Tickets.Dtos;

namespace MaintenanceRequestSystem.Application.Tickets.Interfaces;

public interface ITicketCreationService
{
    Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default);
}
