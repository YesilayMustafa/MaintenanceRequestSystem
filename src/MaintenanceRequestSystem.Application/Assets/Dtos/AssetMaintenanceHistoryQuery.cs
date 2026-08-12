namespace MaintenanceRequestSystem.Application.Assets.Dtos;

public sealed class AssetMaintenanceHistoryQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
