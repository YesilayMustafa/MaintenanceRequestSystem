using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Assets.Interfaces;

public interface IAssetRepository
{
    Task<IReadOnlyList<Asset>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Asset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> SerialNumberExistsAsync(
        string serialNumber,
        Guid? excludedAssetId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Asset asset,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}