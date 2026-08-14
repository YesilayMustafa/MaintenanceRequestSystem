namespace MaintenanceRequestSystem.Application.TicketActivity.Dtos;

public sealed record TicketActivityDto(
    Guid Id,
    string Type,
    string Title,
    string Description,
    Guid ActorUserId,
    string ActorFullName,
    DateTime CreatedAt,
    Guid? CommentId,
    Guid? AttachmentId);
