namespace MaintenanceRequestSystem.Application.Notifications.Dtos;

public sealed record NotificationDto(
    Guid Id,
    string Type,
    string Title,
    string Message,
    Guid? TicketId,
    string? TicketNumber,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);
