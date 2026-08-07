using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Yalnızca Admin adına açık bir ticket'ı ilk kez aktif bir Technician kullanıcısına atar.
    /// Domain davranışı durum geçişini ve history kaydını birlikte gerçekleştirir.
    /// </summary>
    public async Task<TicketDto> AssignAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketAssignmentService.AssignAsync(
            id,
            currentUserId,
            currentUserRole,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Yalnızca Admin adına atanmış bir ticket'ı farklı ve aktif bir Technician kullanıcısına yeniden atar.
    /// Durum Assigned kalırken atama değişikliği ve history domain içinde birlikte güncellenir.
    /// </summary>
    public async Task<TicketDto> ReassignAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        AssignTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketAssignmentService.ReassignAsync(
            id,
            currentUserId,
            currentUserRole,
            request,
            cancellationToken);
    }
}
