using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Kullanıcının aktif bir Admin olduğunu doğrular ve
    /// talebin önceliğini günceller.
    /// </summary>
    public async Task<TicketDto> ChangePriorityAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ChangeTicketPriorityRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketAdministrationService.ChangePriorityAsync(
            id,
            currentUserId,
            currentUserRole,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Aktif Admin kontrolünü yaptıktan sonra tamamlanmış talebi
    /// fiziksel olarak silmeden pasifleştirir.
    /// </summary>
    public async Task SoftDeleteAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        await _ticketAdministrationService.SoftDeleteAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }
}
