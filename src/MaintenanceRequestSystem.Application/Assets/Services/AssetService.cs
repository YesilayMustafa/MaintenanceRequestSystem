using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Assets.Services;

public sealed class AssetService : IAssetService
{
    private readonly IAssetRepository _assetRepository;
    private readonly IDepartmentRepository
        _departmentRepository;

    public AssetService(
        IAssetRepository assetRepository,
        IDepartmentRepository departmentRepository)
    {
        _assetRepository = assetRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<IReadOnlyList<AssetDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var assets =
            await _assetRepository.GetAllAsync(
                cancellationToken);

        return assets
            .Select(asset => MapToDto(asset))
            .ToList();
    }

    public async Task<AssetDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);

        var asset =
            await _assetRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (asset is null)
        {
            throw new KeyNotFoundException(
                "Cihaz bulunamadı.");
        }

        return MapToDto(asset);
    }

    public async Task<AssetDto> CreateAsync(
        CreateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var serialNumberExists =
            await _assetRepository.SerialNumberExistsAsync(
                request.SerialNumber,
                cancellationToken: cancellationToken);

        if (serialNumberExists)
        {
            throw new ConflictException(
                "Bu seri numarasıyla kayıtlı bir cihaz zaten var.");
        }

        var department =
            await _departmentRepository.GetByIdAsync(
                request.DepartmentId,
                cancellationToken);

        if (department is null)
        {
            throw new KeyNotFoundException(
                "Seçilen departman bulunamadı.");
        }

        if (!department.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir departmana cihaz atanamaz.");
        }

        var asset = new Asset(
            request.Name,
            request.SerialNumber,
            request.Type,
            request.DepartmentId,
            request.Location);

        await _assetRepository.AddAsync(
            asset,
            cancellationToken);

        await _assetRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            asset,
            department.Name);
    }

    public async Task<AssetDto> UpdateAsync(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        ArgumentNullException.ThrowIfNull(request);

        var asset =
            await _assetRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (asset is null)
        {
            throw new KeyNotFoundException(
                "Cihaz bulunamadı.");
        }

        var serialNumberExists =
            await _assetRepository.SerialNumberExistsAsync(
                request.SerialNumber,
                excludedAssetId: id,
                cancellationToken: cancellationToken);

        if (serialNumberExists)
        {
            throw new ConflictException(
                "Bu seri numarası başka bir cihaz tarafından kullanılıyor.");
        }

        var department =
            await _departmentRepository.GetByIdAsync(
                request.DepartmentId,
                cancellationToken);

        if (department is null)
        {
            throw new KeyNotFoundException(
                "Seçilen departman bulunamadı.");
        }

        if (!department.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir departmana cihaz atanamaz.");
        }

        asset.UpdateDetails(
            request.Name,
            request.SerialNumber,
            request.Type,
            request.DepartmentId,
            request.Location);

        await _assetRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            asset,
            department.Name);
    }

    public async Task ChangeStatusAsync(
        Guid id,
        ChangeAssetStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        ArgumentNullException.ThrowIfNull(request);

        var asset =
            await _assetRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (asset is null)
        {
            throw new KeyNotFoundException(
                "Cihaz bulunamadı.");
        }

        if (request.IsActive)
        {
            asset.Activate();
        }
        else
        {
            asset.Deactivate();
        }

        await _assetRepository.SaveChangesAsync(
            cancellationToken);
    }

    private static void EnsureValidId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                "Geçerli bir cihaz kimliği gereklidir.");
        }
    }

    private static AssetDto MapToDto(
        Asset asset,
        string? departmentName = null)
    {
        return new AssetDto(
            asset.Id,
            asset.Name,
            asset.SerialNumber,
            asset.Type.ToString(),
            asset.Location,
            asset.DepartmentId,
            departmentName ??
            asset.Department?.Name ??
            string.Empty,
            asset.IsActive,
            asset.CreatedAt,
            asset.UpdatedAt);
    }
}