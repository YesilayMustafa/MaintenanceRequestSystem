using MaintenanceRequestSystem.Application.Tickets.Dtos;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Aktif kullanıcı ve cihaz doğrulamalarından sonra yeni ticket oluşturur.
    /// </summary>
    public async Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketCreationService.CreateAsync(
            createdByUserId,
            request,
            cancellationToken);
    }
}