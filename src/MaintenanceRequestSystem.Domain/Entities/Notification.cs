using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class Notification
{
    public const int MaxTitleLength = 150;
    public const int MaxMessageLength = 500;

    private Notification()
    {
    }

    public Notification(
        Guid userId,
        NotificationType type,
        string title,
        string message,
        Guid? ticketId = null)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kullanıcı kimliği gereklidir.",
                nameof(userId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Geçerli bir bildirim türü gereklidir.");
        }

        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException(
                "Talep kimliği boş olamaz.",
                nameof(ticketId));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TicketId = ticketId;
        Type = type;
        Title = Normalize(title, MaxTitleLength, "Bildirim başlığı");
        Message = Normalize(message, MaxMessageLength, "Bildirim mesajı");
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;
    public Guid? TicketId { get; private set; }
    public Ticket? Ticket { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime? ReadAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public void MarkAsRead()
    {
        if (IsRead)
        {
            return;
        }

        IsRead = true;
        ReadAt = DateTime.UtcNow;
    }

    private static string Normalize(
        string value,
        int maximumLength,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{fieldName} gereklidir.");
        }

        var normalized = value.Trim();

        if (normalized.Length > maximumLength)
        {
            throw new ArgumentException(
                $"{fieldName} en fazla {maximumLength} karakter olabilir.");
        }

        return normalized;
    }
}
