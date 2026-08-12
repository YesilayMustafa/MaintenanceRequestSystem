namespace MaintenanceRequestSystem.Application.TicketAttachments.Models;

public sealed record AttachmentUpload(
    Stream Content,
    string FileName,
    string ContentType,
    long SizeBytes);
