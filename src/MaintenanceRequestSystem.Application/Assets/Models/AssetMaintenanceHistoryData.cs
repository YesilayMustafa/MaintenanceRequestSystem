using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Assets.Models;

public sealed record AssetMaintenanceHistoryData(
    AssetMaintenanceAssetData Asset,
    AssetMaintenanceSummaryData Summary,
    IReadOnlyList<AssetMaintenanceTicketData> Tickets,
    int TotalCount);

public sealed record AssetMaintenanceAssetData(
    Guid Id,
    string Name,
    string SerialNumber,
    AssetType Type);

public sealed record AssetMaintenanceSummaryData(
    int TotalTicketCount,
    int ActiveTicketCount,
    int ResolvedTicketCount,
    int ClosedTicketCount,
    int CriticalTicketCount,
    DateTime? LastTicketCreatedAt);

public sealed record AssetMaintenanceTicketData(
    Guid Id,
    string TicketNumber,
    string Title,
    Guid CategoryId,
    string CategoryName,
    TicketStatus Status,
    TicketPriority Priority,
    DateTime CreatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    string CreatedByFullName,
    string? AssignedTechnicianFullName);
