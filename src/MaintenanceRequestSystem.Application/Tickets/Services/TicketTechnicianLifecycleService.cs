using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed class TicketTechnicianLifecycleService
    : ITicketTechnicianLifecycleService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public TicketTechnicianLifecycleService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Yalnızca teknik personel talepleri işleme alabilir.");
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

        var technician =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (technician is null)
        {
            throw new KeyNotFoundException(
                "Teknik personel bulunamadı.");
        }

        if (!technician.IsActive)
        {
            throw new ForbiddenException(
                "Pasif teknik personel talepleri işleme alamaz.");
        }

        if (technician.Role != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Kullanıcı teknik personel rolünde değildir.");
        }

        ticket.StartProgress(
            currentUserId);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            assignedTechnician: technician);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Yalnızca teknik personel talepleri beklemeye alabilir.");
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

        var technician =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (technician is null)
        {
            throw new KeyNotFoundException(
                "Teknik personel bulunamadı.");
        }

        if (!technician.IsActive)
        {
            throw new ForbiddenException(
                "Pasif teknik personel talebi beklemeye alamaz.");
        }

        if (technician.Role != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Kullanıcı teknik personel rolünde değildir.");
        }

        ticket.PutOnHold(
            request.Reason,
            currentUserId);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            assignedTechnician: technician);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Yalnızca teknik personel talepte işleme devam edebilir.");
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

        var technician =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (technician is null)
        {
            throw new KeyNotFoundException(
                "Teknik personel bulunamadı.");
        }

        if (!technician.IsActive)
        {
            throw new ForbiddenException(
                "Pasif teknik personel talepte işleme devam edemez.");
        }

        ticket.Resume(
            currentUserId);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            assignedTechnician: technician);
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
        TicketServiceGuards.EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        TicketServiceGuards.EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        TicketServiceGuards.EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Yalnızca teknik personel talepleri çözebilir.");
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

        var technician =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (technician is null)
        {
            throw new KeyNotFoundException(
                "Teknik personel bulunamadı.");
        }

        if (!technician.IsActive)
        {
            throw new ForbiddenException(
                "Pasif teknik personel talepleri çözemez.");
        }

        if (technician.Role != UserRole.Technician)
        {
            throw new ForbiddenException(
                "Kullanıcı teknik personel rolünde değildir.");
        }

        // Ticket entity'si durumun InProgress olduğunu ve işlemi yapan
        // kullanıcının atanmış teknisyen olduğunu ayrıca doğrular.
        ticket.Resolve(
            request.ResolutionDescription,
            currentUserId);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return TicketDtoMapper.MapToDto(
            ticket,
            assignedTechnician: technician);
    }
}
