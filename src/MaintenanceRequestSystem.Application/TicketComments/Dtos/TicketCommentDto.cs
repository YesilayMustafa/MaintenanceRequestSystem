namespace MaintenanceRequestSystem.Application.TicketComments.Dtos;

public sealed record TicketCommentDto(
    Guid Id,
    Guid TicketId,
    Guid UserId,
    string UserFullName,
    string UserRole,
    string Content,
    DateTime CreatedAt);