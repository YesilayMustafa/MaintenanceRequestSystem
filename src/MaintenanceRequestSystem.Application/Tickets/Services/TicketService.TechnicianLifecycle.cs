using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Kullanıcı rolünü ve aktifliğini doğrular, ardından talebin
    /// Assigned durumundan InProgress durumuna geçmesini sağlar.
    /// </summary>
    public async Task<TicketDto> StartProgressAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        return await _ticketTechnicianLifecycleService.StartProgressAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }

    /// <summary>
    /// Kullanıcı ve ticket kurallarını doğruladıktan sonra talebi
    /// InProgress durumundan Waiting durumuna geçirir.
    /// </summary>
    public async Task<TicketDto> PutOnHoldAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        PutTicketOnHoldRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketTechnicianLifecycleService.PutOnHoldAsync(
            id,
            currentUserId,
            currentUserRole,
            request,
            cancellationToken);
    }

    /// <summary>
    /// Kullanıcı ve ticket kurallarını doğruladıktan sonra beklemedeki
    /// talebi yeniden InProgress durumuna geçirir.
    /// </summary>
    public async Task<TicketDto> ResumeAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        return await _ticketTechnicianLifecycleService.ResumeAsync(
            id,
            currentUserId,
            currentUserRole,
            cancellationToken);
    }

    /// <summary>
    /// Kullanıcı ve ticket kurallarını doğruladıktan sonra talebi
    /// InProgress durumundan Resolved durumuna geçirir.
    /// </summary>
    public async Task<TicketDto> ResolveAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        ResolveTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        return await _ticketTechnicianLifecycleService.ResolveAsync(
            id,
            currentUserId,
            currentUserRole,
            request,
            cancellationToken);
    }
}
