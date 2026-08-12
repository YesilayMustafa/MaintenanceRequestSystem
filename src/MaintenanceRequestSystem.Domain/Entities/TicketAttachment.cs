namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class TicketAttachment
{
    public const int MaxOriginalFileNameLength = 255;
    public const int MaxStorageKeyLength = 100;
    public const int MaxContentTypeLength = 100;

    private TicketAttachment()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public TicketAttachment(
        Guid ticketId,
        Guid uploadedByUserId,
        string originalFileName,
        string storageKey,
        string contentType,
        long sizeBytes)
    {
        EnsureValidId(ticketId, nameof(ticketId));
        EnsureValidId(uploadedByUserId, nameof(uploadedByUserId));

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sizeBytes),
                "Dosya boyutu sıfırdan büyük olmalıdır.");
        }

        Id = Guid.NewGuid();
        TicketId = ticketId;
        UploadedByUserId = uploadedByUserId;
        OriginalFileName = NormalizeRequired(
            originalFileName,
            MaxOriginalFileNameLength,
            "Orijinal dosya adı",
            nameof(originalFileName));
        StorageKey = NormalizeRequired(
            storageKey,
            MaxStorageKeyLength,
            "Dosya saklama anahtarı",
            nameof(storageKey));
        ContentType = NormalizeRequired(
            contentType,
            MaxContentTypeLength,
            "İçerik türü",
            nameof(contentType));
        SizeBytes = sizeBytes;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Ticket Ticket { get; private set; } = null!;

    public Guid UploadedByUserId { get; private set; }

    public User UploadedByUser { get; private set; } = null!;

    public string OriginalFileName { get; private set; } = string.Empty;

    public string StorageKey { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private static void EnsureValidId(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kimlik gereklidir.",
                parameterName);
        }
    }

    private static string NormalizeRequired(
        string value,
        int maxLength,
        string displayName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} boş olamaz.",
                parameterName);
        }

        var normalized = value.Trim();

        if (normalized.Length > maxLength)
        {
            throw new ArgumentException(
                $"{displayName} en fazla {maxLength} karakter olabilir.",
                parameterName);
        }

        return normalized;
    }
}
