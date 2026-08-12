using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketAssignmentService : ITicketAssignmentService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public TicketAssignmentService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        // Yetki ve hedef kullanıcı uygunluğu, domain state'i değişmeden önce tamamen doğrulanır.
        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Yalnızca yöneticiler talep atayabilir.");
        }

        ArgumentNullException.ThrowIfNull(request);

        TicketServiceGuards.EnsureValidId(
            request.TechnicianId,
            "Geçerli bir teknik personel kimliği gereklidir.");

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        var technician =
            await _userRepository.GetByIdAsync(
                request.TechnicianId,
                cancellationToken);

        if (technician is null)
        {
            throw new KeyNotFoundException(
                "Teknik personel bulunamadı.");
        }

        if (!technician.IsOperational)
        {
            throw new RequestValidationException(
                "Yalnızca kullanıma hazır aktif bir teknik personele talep atanabilir.");
        }

        if (technician.Role != UserRole.Technician)
        {
            throw new RequestValidationException(
                "Talep yalnızca teknik personel rolündeki kullanıcıya atanabilir.");
        }



        var oldStatus =
    ticket.Status;

        var oldAssignedTechnicianId =
            ticket.AssignedTechnicianId;

        ticket.Assign(
            request.TechnicianId,
            currentUserId);

        await _auditLogService.AddAsync(
            currentUserId,
            "TicketAssigned",
            nameof(Ticket),
            ticket.Id.ToString(),
            new
            {
                Status = oldStatus,
                AssignedTechnicianId =
                    oldAssignedTechnicianId
            },
            new
            {
                Status = ticket.Status,
                AssignedTechnicianId =
                    ticket.AssignedTechnicianId
            },
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            assignedTechnician: technician);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        // Yetki ve hedef kullanıcı uygunluğu, mevcut atama değiştirilmeden önce tamamen doğrulanır.
        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Yalnızca yöneticiler talepleri yeniden atayabilir.");
        }

        ArgumentNullException.ThrowIfNull(request);

        TicketServiceGuards.EnsureValidId(
            request.TechnicianId,
            "Geçerli bir teknik personel kimliği gereklidir.");

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        var technician =
            await _userRepository.GetByIdAsync(
                request.TechnicianId,
                cancellationToken);

        if (technician is null)
        {
            throw new KeyNotFoundException(
                "Teknik personel bulunamadı.");
        }

        if (!technician.IsOperational)
        {
            throw new RequestValidationException(
                "Yalnızca kullanıma hazır aktif bir teknik personele talep atanabilir.");
        }

        if (technician.Role != UserRole.Technician)
        {
            throw new RequestValidationException(
                "Talep yalnızca teknik personel rolündeki kullanıcıya atanabilir.");
        }

        var oldAssignedTechnicianId =
    ticket.AssignedTechnicianId;

        ticket.Reassign(
            technician.Id,
            currentUserId);

        await _auditLogService.AddAsync(
    currentUserId,
    "TicketReassigned",
    nameof(Ticket),
    ticket.Id.ToString(),
    new
    {
        AssignedTechnicianId =
            oldAssignedTechnicianId
    },
    new
    {
        AssignedTechnicianId =
            ticket.AssignedTechnicianId
    },
    cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            assignedTechnician: technician);
    }
}
