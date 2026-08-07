using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketAdministrationService
    : ITicketAdministrationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public TicketAdministrationService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Talep önceliğini yalnızca yönetici değiştirebilir.");
        }

        ArgumentNullException.ThrowIfNull(request);

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        var admin =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (admin is null)
        {
            throw new KeyNotFoundException(
                "Yönetici kullanıcı bulunamadı.");
        }

        if (!admin.IsActive)
        {
            throw new ForbiddenException(
                "Pasif yöneticiler talep önceliğini değiştiremez.");
        }

        if (admin.Role != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Kullanıcı yönetici rolünde değildir.");
        }

        var oldPriority =
            ticket.Priority;

        ticket.ChangePriority(
            request.Priority,
            currentUserId);

        await _auditLogService.AddAsync(
            currentUserId,
            "TicketPriorityChanged",
            nameof(Ticket),
            ticket.Id.ToString(),
            new
            {
                Priority = oldPriority
            },
            new
            {
                Priority = ticket.Priority
            },
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(ticket);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Talepleri yalnızca yönetici pasifleştirebilir.");
        }

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        var admin =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (admin is null)
        {
            throw new KeyNotFoundException(
                "Yönetici kullanıcı bulunamadı.");
        }

        if (!admin.IsActive ||
            admin.Role != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Yalnızca aktif yöneticiler talep pasifleştirebilir.");
        }

        var oldIsDeleted =
            ticket.IsDeleted;

        var oldDeletedAt =
            ticket.DeletedAt;

        var oldDeletedByUserId =
            ticket.DeletedByUserId;

        ticket.SoftDelete(
            currentUserId);

        await _auditLogService.AddAsync(
            currentUserId,
            "TicketSoftDeleted",
            nameof(Ticket),
            ticket.Id.ToString(),
            new
            {
                IsDeleted = oldIsDeleted,
                DeletedAt = oldDeletedAt,
                DeletedByUserId =
                    oldDeletedByUserId
            },
            new
            {
                ticket.IsDeleted,
                ticket.DeletedAt,
                ticket.DeletedByUserId
            },
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);
    }
}
