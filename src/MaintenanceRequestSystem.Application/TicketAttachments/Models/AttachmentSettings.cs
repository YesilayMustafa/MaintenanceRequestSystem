namespace MaintenanceRequestSystem.Application.TicketAttachments.Models;

public sealed class AttachmentSettings
{
    public const string SectionName = "Attachments";

    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;

    public int MaxAttachmentsPerTicket { get; init; } = 10;

    public IReadOnlySet<string> AllowedExtensions { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp",
            ".pdf"
        };

    public IReadOnlySet<string> AllowedContentTypes { get; init; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/webp",
            "application/pdf"
        };

    public string StorageRootPath { get; init; } = string.Empty;
}
