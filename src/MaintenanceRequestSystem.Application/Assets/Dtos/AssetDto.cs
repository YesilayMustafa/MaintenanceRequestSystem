namespace MaintenanceRequestSystem.Application.Assets.Dtos;

public sealed record AssetDto(
    Guid Id,
    string Name,
    string SerialNumber,
    string Type,
    string? Location,
    Guid DepartmentId,
    string DepartmentName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);