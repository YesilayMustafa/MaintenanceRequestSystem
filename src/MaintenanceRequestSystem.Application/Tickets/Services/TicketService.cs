using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

/// <summary>
/// Ticket use case'lerini, yetki kontrollerini ve repository koordinasyonunu yürütür.
/// </summary>
public sealed partial class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;




    public TicketService(
        ITicketRepository ticketRepository,
        IAssetRepository assetRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _userRepository = userRepository;

        ArgumentNullException.ThrowIfNull(auditLogService);

        _auditLogService =
            auditLogService;
    }

    /// <summary>
    /// Aktif kullanıcı ve cihaz doğrulamalarından sonra yeni ticket oluşturur.
    /// </summary>
    public async Task<TicketDto> CreateAsync(
        Guid createdByUserId,
        CreateTicketRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            createdByUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        ArgumentNullException.ThrowIfNull(request);

        EnsureValidId(
    request.AssetId,
    "Geçerli bir cihaz kimliği gereklidir.");

        var user =
            await _userRepository.GetByIdAsync(
                createdByUserId,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Talebi oluşturan kullanıcı bulunamadı.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar talep oluşturamaz.");
        }

        var asset =
            await _assetRepository.GetByIdAsync(
                request.AssetId,
                cancellationToken);

        if (asset is null)
        {
            throw new KeyNotFoundException(
                "Seçilen cihaz bulunamadı.");
        }

        if (!asset.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir cihaz için yeni talep oluşturulamaz.");
        }

        var ticket = new Ticket(
            request.AssetId,
            createdByUserId,
            request.Title,
            request.Description,
            request.Priority);

        await _ticketRepository.AddAsync(
            ticket,
            cancellationToken);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            ticket,
            asset,
            user);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        return MapToDto(
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        return MapToDto(
            ticket,
            assignedTechnician: technician);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(ticket);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(ticket);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        return MapToDto(ticket);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(ticket);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        return MapToDto(
            ticket,
            assignedTechnician: technician);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

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

        return MapToDto(
            ticket,
            assignedTechnician: technician);
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

        // Yetki ve hedef kullanıcı uygunluğu, domain state'i değişmeden önce tamamen doğrulanır.
        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Yalnızca yöneticiler talep atayabilir.");
        }

        ArgumentNullException.ThrowIfNull(request);

        EnsureValidId(
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

        if (!technician.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir kullanıcıya talep atanamaz.");
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

        return MapToDto(
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
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

        // Yetki ve hedef kullanıcı uygunluğu, mevcut atama değiştirilmeden önce tamamen doğrulanır.
        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Yalnızca yöneticiler talepleri yeniden atayabilir.");
        }

        ArgumentNullException.ThrowIfNull(request);

        EnsureValidId(
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

        if (!technician.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir kullanıcıya talep atanamaz.");
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

        return MapToDto(
            ticket,
            assignedTechnician: technician);
    }






    private static void EnsureValidId(
    Guid id,
    string errorMessage)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                errorMessage);
        }
    }

    private static void EnsureSupportedRole(
        UserRole role)
    {
        if (!Enum.IsDefined(
                typeof(UserRole),
                role))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }
    }

    private static TicketDto MapToDto(
        Ticket ticket,
        Asset? asset = null,
        User? createdByUser = null,
        User? assignedTechnician = null)
    {

        var ticketAssignedTechnician =
    assignedTechnician ??
    ticket.AssignedTechnician;
        var ticketAsset =
            asset ?? ticket.Asset;

        var ticketCreator =
            createdByUser ?? ticket.CreatedByUser;

        return new TicketDto(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.AssetId,
            ticketAsset.Name,
            ticketAsset.SerialNumber,
            ticket.CreatedByUserId,
            ticketCreator.FullName,
            ticket.AssignedTechnicianId,
            ticketAssignedTechnician?.FullName,
            ticket.WaitingReason,
            ticket.ResolutionDescription,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            ticket.ResolvedAt,
            ticket.ClosedAt);
    }





}
