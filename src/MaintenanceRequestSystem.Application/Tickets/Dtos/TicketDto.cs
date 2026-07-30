namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed record TicketDto(
    Guid Id,
    string Title,
    string Description,
    string Priority,
    string Status,
    Guid AssetId,
    string AssetName,
    string AssetSerialNumber,
    Guid CreatedByUserId,
    string CreatedByFullName,
    Guid? AssignedTechnicianId,
    string? AssignedTechnicianFullName,
    string? WaitingReason,
    string? ResolutionDescription,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt);