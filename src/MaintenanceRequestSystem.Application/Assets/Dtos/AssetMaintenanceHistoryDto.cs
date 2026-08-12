using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Application.Assets.Dtos;

public sealed record AssetMaintenanceHistoryDto(
    AssetMaintenanceHistoryAssetDto Asset,
    AssetMaintenanceSummaryDto Summary,
    PagedResult<AssetMaintenanceTicketDto> Tickets);

public sealed record AssetMaintenanceHistoryAssetDto(
    Guid Id,
    string Name,
    string SerialNumber,
    string Type);

public sealed record AssetMaintenanceSummaryDto(
    int TotalTicketCount,
    int ActiveTicketCount,
    int ResolvedTicketCount,
    int ClosedTicketCount,
    int CriticalTicketCount,
    DateTime? LastTicketCreatedAt);

public sealed record AssetMaintenanceTicketDto(
    Guid Id,
    string TicketNumber,
    string Title,
    Guid CategoryId,
    string CategoryName,
    string Status,
    string Priority,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string CreatedByFullName,
    string? AssignedTechnicianFullName);
