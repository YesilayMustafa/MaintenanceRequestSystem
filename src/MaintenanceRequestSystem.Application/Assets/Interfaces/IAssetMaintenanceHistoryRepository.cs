using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Models;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Assets.Interfaces;

public interface IAssetMaintenanceHistoryRepository
{
    Task<AssetMaintenanceHistoryData?> GetAsync(
        Guid assetId,
        Guid currentUserId,
        UserRole currentUserRole,
        AssetMaintenanceHistoryQuery query,
        CancellationToken cancellationToken = default);
}
