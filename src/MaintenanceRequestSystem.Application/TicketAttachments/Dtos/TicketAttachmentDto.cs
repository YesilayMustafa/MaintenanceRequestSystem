namespace MaintenanceRequestSystem.Application.TicketAttachments.Dtos;

public sealed record TicketAttachmentDto(
    Guid Id,
    Guid TicketId,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    Guid UploadedByUserId,
    string UploadedByFullName,
    DateTime CreatedAt);
