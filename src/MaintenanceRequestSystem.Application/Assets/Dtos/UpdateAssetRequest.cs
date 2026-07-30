using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Assets.Dtos;

public sealed class UpdateAssetRequest
{
    public string Name { get; init; } = string.Empty;

    public string SerialNumber { get; init; } = string.Empty;

    public AssetType Type { get; init; }

    public Guid DepartmentId { get; init; }

    public string? Location { get; init; }
}