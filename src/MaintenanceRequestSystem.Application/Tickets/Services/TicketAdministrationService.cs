using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.Sla.Models;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketAdministrationService
    : ITicketAdministrationService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationWriter _notificationWriter;
    private readonly SlaOptions _slaOptions;

    public TicketAdministrationService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        INotificationWriter? notificationWriter = null,
        SlaOptions? slaOptions = null)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _notificationWriter = notificationWriter ?? new NullNotificationWriter();
        _slaOptions = slaOptions ?? new SlaOptions();
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
            currentUserId,
            _slaOptions.GetTarget(request.Priority));

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

        await _notificationWriter.AddAsync(
            currentUserId,
            GetTicketParticipants(ticket),
            NotificationType.TicketPriorityChanged,
            "Talep önceliği değiştirildi",
            $"{ticket.TicketNumber} numaralı talebin önceliği değiştirildi.",
            ticket.Id,
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

    private static IEnumerable<Guid> GetTicketParticipants(Ticket ticket)
    {
        yield return ticket.CreatedByUserId;

        if (ticket.AssignedTechnicianId.HasValue)
        {
            yield return ticket.AssignedTechnicianId.Value;
        }
    }
}
