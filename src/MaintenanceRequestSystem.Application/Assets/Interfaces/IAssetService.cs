using MaintenanceRequestSystem.Application.Assets.Dtos;

namespace MaintenanceRequestSystem.Application.Assets.Interfaces;

public interface IAssetService
{
    Task<IReadOnlyList<AssetDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<AssetDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AssetDto> CreateAsync(
        CreateAssetRequest request,
        CancellationToken cancellationToken = default);

    Task<AssetDto> UpdateAsync(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        Guid id,
        ChangeAssetStatusRequest request,
        CancellationToken cancellationToken = default);
}