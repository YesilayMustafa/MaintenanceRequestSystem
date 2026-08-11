namespace MaintenanceRequestSystem.Application.Dashboard.Dtos;

public sealed record DashboardTicketDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string Status,
    string Priority,
    string AssetName,
    DateTime CreatedAt,
    string? AssignedTechnicianFullName);
