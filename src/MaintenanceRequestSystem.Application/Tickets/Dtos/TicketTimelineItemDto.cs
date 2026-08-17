namespace MaintenanceRequestSystem.Application.Tickets.Dtos;

public sealed record TicketTimelineItemDto(
    Guid Id,
    string TicketNumber,
    string Title,
    string Status,
    string Priority,
    Guid CategoryId,
    string CategoryName,
    Guid? AssignedTechnicianId,
    string? AssignedTechnicianFullName,
    DateTime CreatedAt,
    DateTime SlaDueAt,
    string SlaStatus);
