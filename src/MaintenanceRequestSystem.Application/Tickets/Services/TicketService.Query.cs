using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Ticket listesini rol bazlı kapsam, filtre, sıralama ve sayfalama ile getirir.
    /// </summary>
    public async Task<PagedResult<TicketDto>> GetPagedAsync(
    Guid currentUserId,
    UserRole currentUserRole,
    TicketListQuery query,
    CancellationToken cancellationToken = default)
    {
        return await _ticketQueryService.GetPagedAsync(
            currentUserId,
            currentUserRole,
            query,
            cancellationToken);
    }

    /// <summary>
    /// Talep sahibi, atanmış teknik personel veya Admin için
    /// talebin durum geçmişini getirir.
    /// </summary>
    public async Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        return await _ticketQueryService.GetHistoryAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }

    /// <summary>
    /// Rol bazlı erişim kuralını uygulayarak ticket detayını getirir.
    /// </summary>
    public async Task<TicketDto> GetByIdAsync(
    Guid id,
    Guid currentUserId,
    UserRole currentUserRole,
    CancellationToken cancellationToken = default)
    {
        return await _ticketQueryService.GetByIdAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }
}