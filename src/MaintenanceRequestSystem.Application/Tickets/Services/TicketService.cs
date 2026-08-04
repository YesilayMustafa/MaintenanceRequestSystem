using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

/// <summary>
/// Ticket use case'lerini, yetki kontrollerini ve repository koordinasyonunu yürütür.
/// </summary>
public sealed class TicketService : ITicketService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IAssetRepository _assetRepository;
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// Ticket servisini gerekli repository bağımlılıklarıyla oluşturur.
    /// </summary>
    public TicketService(
        ITicketRepository ticketRepository,
        IAssetRepository assetRepository,
        IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _assetRepository = assetRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Ticket listesini rol bazlı kapsam, filtre, sıralama ve sayfalama ile getirir.
    /// </summary>
    public async Task<PagedResult<TicketDto>> GetPagedAsync(
    Guid currentUserId,
    UserRole currentUserRole,
    TicketListQuery query,
    CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        ArgumentNullException.ThrowIfNull(query);

        ValidateListQuery(
            currentUserRole,
            query);

        var result =
            await _ticketRepository.GetPagedAsync(
                currentUserId,
                currentUserRole,
                query,
                cancellationToken);

        var items =
            result.Items
                .Select(ticket => MapToDto(ticket))
                .ToList();

        var totalPages =
            result.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalCount /
                    (double)query.PageSize);

        return new PagedResult<TicketDto>(
            items,
            query.PageNumber,
            query.PageSize,
            result.TotalCount,
            totalPages);
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

        ticket.Assign(
            technician.Id,
            currentUserId);

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

        ticket.Reassign(
            technician.Id,
            currentUserId);

        await _ticketRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            ticket,
            assignedTechnician: technician);
    }



    /// <summary>
    /// Rol bazlı erişim kuralını uygulayarak ticket detayını getirir.
    /// </summary>
    public async Task<TicketDto> GetByIdAsync(
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

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talebi görüntüleyemezsiniz.");
        }

        return MapToDto(ticket);
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


    private static void ValidateListQuery(
    UserRole currentUserRole,
    TicketListQuery query)
    {
        if (!Enum.IsDefined(
                typeof(UserRole),
                currentUserRole))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }

        if (query.PageNumber < 1)
        {
            throw new RequestValidationException(
                "Sayfa numarası en az 1 olmalıdır.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }
        var offset =
    ((long)query.PageNumber - 1L) *
    query.PageSize;

        if (offset > int.MaxValue)
        {
            throw new RequestValidationException(
                "İstenen sayfa numarası desteklenen sınırı aşıyor.");
        }

        if (query.Status.HasValue &&
    !Enum.IsDefined(
        typeof(TicketStatus),
        query.Status.Value))
        {
            throw new RequestValidationException(
                "Geçersiz talep durumu.");
        }
        if (query.Priority.HasValue &&
            !Enum.IsDefined(
                typeof(TicketPriority),
                query.Priority.Value))
        {
            throw new RequestValidationException(
                "Geçersiz talep önceliği.");
        }

        if (query.AssetId == Guid.Empty)
        {
            throw new RequestValidationException(
                "Geçerli bir cihaz kimliği gereklidir.");
        }

        var allowedSortFields =
            new[]
            {
            "createdat",
            "title",
            "priority",
            "status"
            };

        var normalizedSortBy =
            query.SortBy.Trim().ToLowerInvariant();

        if (!allowedSortFields.Contains(
                normalizedSortBy))
        {
            throw new RequestValidationException(
                "Sıralama alanı createdAt, title, priority veya status olmalıdır.");
        }
    }



}
