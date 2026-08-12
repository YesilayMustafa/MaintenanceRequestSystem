using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Assets.Interfaces;

public interface IAssetMaintenanceHistoryService
{
    Task<AssetMaintenanceHistoryDto> GetAsync(
        Guid assetId,
        Guid currentUserId,
        UserRole currentUserRole,
        AssetMaintenanceHistoryQuery query,
        CancellationToken cancellationToken = default);
}
