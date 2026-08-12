namespace MaintenanceRequestSystem.Application.TicketAttachments.Models;

public sealed record AttachmentDownload(
    Stream Content,
    string OriginalFileName,
    string ContentType);
