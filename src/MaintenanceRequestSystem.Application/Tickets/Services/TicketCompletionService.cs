using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketCompletionService : ITicketCompletionService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;
    private readonly INotificationWriter _notificationWriter;

    public TicketCompletionService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService,
        INotificationWriter? notificationWriter = null)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
        _notificationWriter = notificationWriter ?? new NullNotificationWriter();
    }

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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole is not
            (UserRole.Employee or UserRole.Admin))
        {
            throw new ForbiddenException(
                "Talebi yalnızca talep sahibi veya yönetici kapatabilir.");
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

        var currentUser =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException(
                "İşlemi yapan kullanıcı bulunamadı.");
        }

        if (!currentUser.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar talep kapatamaz.");
        }

        // Employee yalnızca kendisinin oluşturduğu talebi kapatabilir.
        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talebi kapatamazsınız.");
        }

        ticket.Close(
            currentUserId);

        await AddAssignedTechnicianNotificationAsync(
            ticket,
            currentUserId,
            NotificationType.TicketClosed,
            "Talep kapatıldı",
            $"{ticket.TicketNumber} numaralı talep kapatıldı.",
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(ticket);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole is not
            (UserRole.Employee or UserRole.Admin))
        {
            throw new ForbiddenException(
                "Talebi yalnızca talep sahibi veya yönetici yeniden açabilir.");
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

        var currentUser =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException(
                "İşlemi yapan kullanıcı bulunamadı.");
        }

        if (!currentUser.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar talebi yeniden açamaz.");
        }

        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talebi yeniden açamazsınız.");
        }

        ticket.Reopen(
            request.Reason,
            currentUserId);

        await AddAssignedTechnicianNotificationAsync(
            ticket,
            currentUserId,
            NotificationType.TicketReopened,
            "Talep yeniden açıldı",
            $"{ticket.TicketNumber} numaralı talep yeniden açıldı.",
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(ticket);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole is not
            (UserRole.Employee or UserRole.Admin))
        {
            throw new ForbiddenException(
                "Talebi yalnızca talep sahibi veya yönetici iptal edebilir.");
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

        var currentUser =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException(
                "İşlemi yapan kullanıcı bulunamadı.");
        }

        if (!currentUser.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar talep iptal edemez.");
        }

        if (currentUserRole == UserRole.Employee)
        {
            if (ticket.CreatedByUserId != currentUserId)
            {
                throw new ForbiddenException(
                    "Başka bir kullanıcıya ait talebi iptal edemezsiniz.");
            }

            if (ticket.Status != TicketStatus.Open)
            {
                throw new ForbiddenException(
                    "Talep sahibi yalnızca açık durumdaki talebini iptal edebilir.");
            }
        }

        // Audit kaydında değişiklik öncesindeki durum kullanılacak.
        var oldStatus =
            ticket.Status;

        // Domain katmanı yalnızca Open, Assigned ve Waiting
        // durumlarından Cancelled geçişine izin verir.
        ticket.Cancel(
            currentUserId);

        await _auditLogService.AddAsync(
            currentUserId,
            "TicketCancelled",
            nameof(Ticket),
            ticket.Id.ToString(),
            new
            {
                Status = oldStatus
            },
            new
            {
                Status = ticket.Status
            },
            cancellationToken);

        await AddAssignedTechnicianNotificationAsync(
            ticket,
            currentUserId,
            NotificationType.TicketCancelled,
            "Talep iptal edildi",
            $"{ticket.TicketNumber} numaralı talep iptal edildi.",
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(ticket);
    }

    private Task AddAssignedTechnicianNotificationAsync(
        Ticket ticket,
        Guid actorUserId,
        NotificationType type,
        string title,
        string message,
        CancellationToken cancellationToken)
    {
        return _notificationWriter.AddAsync(
            actorUserId,
            ticket.AssignedTechnicianId.HasValue
                ? [ticket.AssignedTechnicianId.Value]
                : [],
            type,
            title,
            message,
            ticket.Id,
            cancellationToken);
    }
}
