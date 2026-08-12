using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Categories.Dtos;
using MaintenanceRequestSystem.Application.Categories.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Categories.Services;

public sealed class TicketCategoryService : ITicketCategoryService
{
    private readonly ITicketCategoryRepository _categoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAuditLogService _auditLogService;

    public TicketCategoryService(
        ITicketCategoryRepository categoryRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<TicketCategoryDto>> GetAllAsync(
        bool includeInactive,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        EnsureSupportedRole(currentUserRole);

        if (includeInactive && currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Pasif kategorileri yalnızca yöneticiler görüntüleyebilir.");
        }

        var categories = await _categoryRepository.GetAllAsync(
            includeInactive,
            cancellationToken);

        return categories.Select(MapToDto).ToList();
    }

    public async Task<TicketCategoryDto> GetByIdAsync(
        Guid id,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        EnsureSupportedRole(currentUserRole);

        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken);

        if (category is null ||
            (!category.IsActive && currentUserRole != UserRole.Admin))
        {
            throw new KeyNotFoundException("Kategori bulunamadı.");
        }

        return MapToDto(category);
    }

    public async Task<TicketCategoryDto> CreateAsync(
        Guid performedByUserId,
        UserRole currentUserRole,
        CreateTicketCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureActiveAdminAsync(
            performedByUserId,
            currentUserRole,
            cancellationToken);

        ArgumentNullException.ThrowIfNull(request);

        var normalizedName = TicketCategory.NormalizeName(request.Name);

        if (await _categoryRepository.ExistsByNormalizedNameAsync(
                normalizedName,
                cancellationToken: cancellationToken))
        {
            throw new ConflictException(
                "Aynı isimde bir kategori zaten bulunmaktadır.");
        }

        var category = new TicketCategory(
            request.Name,
            request.Description);

        await _categoryRepository.AddAsync(category, cancellationToken);

        await _auditLogService.AddAsync(
            performedByUserId,
            "TicketCategoryCreated",
            nameof(TicketCategory),
            category.Id.ToString(),
            newValues: new
            {
                category.Name,
                category.Description,
                category.IsActive
            },
            cancellationToken: cancellationToken);

        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public async Task<TicketCategoryDto> UpdateAsync(
        Guid id,
        Guid performedByUserId,
        UserRole currentUserRole,
        UpdateTicketCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        await EnsureActiveAdminAsync(
            performedByUserId,
            currentUserRole,
            cancellationToken);

        ArgumentNullException.ThrowIfNull(request);

        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new KeyNotFoundException("Kategori bulunamadı.");

        var normalizedName = TicketCategory.NormalizeName(request.Name);

        if (await _categoryRepository.ExistsByNormalizedNameAsync(
                normalizedName,
                id,
                cancellationToken))
        {
            throw new ConflictException(
                "Aynı isimde başka bir kategori bulunmaktadır.");
        }

        var oldValues = new
        {
            category.Name,
            category.Description
        };

        category.UpdateDetails(request.Name, request.Description);

        await _auditLogService.AddAsync(
            performedByUserId,
            "TicketCategoryUpdated",
            nameof(TicketCategory),
            category.Id.ToString(),
            oldValues,
            new
            {
                category.Name,
                category.Description
            },
            cancellationToken);

        await _categoryRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(category);
    }

    public Task ChangeStatusAsync(
        Guid id,
        Guid performedByUserId,
        UserRole currentUserRole,
        ChangeTicketCategoryStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        ArgumentNullException.ThrowIfNull(request);

        return _categoryRepository.ExecuteInTransactionAsync(
            transactionCancellationToken => ChangeStatusCoreAsync(
                id,
                performedByUserId,
                currentUserRole,
                request,
                transactionCancellationToken),
            cancellationToken);
    }

    private async Task ChangeStatusCoreAsync(
        Guid id,
        Guid performedByUserId,
        UserRole currentUserRole,
        ChangeTicketCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        await EnsureActiveAdminAsync(
            performedByUserId,
            currentUserRole,
            cancellationToken);

        var category = await _categoryRepository.GetByIdAsync(
            id,
            cancellationToken)
            ?? throw new KeyNotFoundException("Kategori bulunamadı.");

        if (!request.IsActive && category.IsActive &&
            await _categoryRepository.CountActiveAsync(cancellationToken) <= 1)
        {
            throw new ConflictException(
                "Sistemde en az bir aktif kategori kalmalıdır.");
        }

        var oldIsActive = category.IsActive;

        if (request.IsActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        if (oldIsActive == category.IsActive)
        {
            return;
        }

        await _auditLogService.AddAsync(
            performedByUserId,
            category.IsActive
                ? "TicketCategoryActivated"
                : "TicketCategoryDeactivated",
            nameof(TicketCategory),
            category.Id.ToString(),
            new { IsActive = oldIsActive },
            new { category.IsActive },
            cancellationToken);

        await _categoryRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureActiveAdminAsync(
        Guid performedByUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken)
    {
        EnsureValidId(performedByUserId);
        EnsureSupportedRole(currentUserRole);

        if (currentUserRole != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Kategori yönetimi yalnızca yöneticiler tarafından yapılabilir.");
        }

        var admin = await _userRepository.GetByIdAsync(
            performedByUserId,
            cancellationToken);

        if (admin is null)
        {
            throw new KeyNotFoundException("Yönetici kullanıcı bulunamadı.");
        }

        if (!admin.IsActive || admin.Role != UserRole.Admin)
        {
            throw new ForbiddenException(
                "Kategori yönetimi için aktif yönetici hesabı gereklidir.");
        }
    }

    private static void EnsureValidId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                "Geçerli bir kategori kimliği gereklidir.");
        }
    }

    private static void EnsureSupportedRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ForbiddenException("Desteklenmeyen kullanıcı rolü.");
        }
    }

    private static TicketCategoryDto MapToDto(TicketCategory category)
    {
        return new TicketCategoryDto(
            category.Id,
            category.Name,
            category.Description,
            category.IsActive,
            category.CreatedAt,
            category.UpdatedAt);
    }
}
