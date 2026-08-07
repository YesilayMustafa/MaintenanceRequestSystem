using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Kullanıcının Admin veya talep sahibi olduğunu doğrular ve
    /// Resolved durumundaki talebi Closed durumuna geçirir.
    /// </summary>
    public async Task<TicketDto> CloseAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        return await _ticketCompletionService.CloseAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }

    /// <summary>
    /// Kullanıcının Admin veya talep sahibi olduğunu doğrular ve
    /// Closed durumundaki talebi yeniden InProgress durumuna geçirir.
    /// </summary>
    public async Task<TicketDto> ReopenAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ReopenTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketCompletionService.ReopenAsync(
            id,
            currentUserId,
            currentUserRole,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Kullanıcının iptal yetkisini ve talebin mevcut durumunu doğrular,
    /// ardından talebi Cancelled durumuna geçirir.
    /// </summary>
    public async Task<TicketDto> CancelAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        return await _ticketCompletionService.CancelAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }
}
