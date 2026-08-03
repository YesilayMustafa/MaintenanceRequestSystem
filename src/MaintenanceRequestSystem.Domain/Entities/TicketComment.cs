namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class TicketComment
{
    public const int MaxContentLength = 2000;

    private TicketComment()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public TicketComment(
        Guid ticketId,
        Guid userId,
        string content)
    {
        EnsureValidTicketId(ticketId);
        EnsureValidUserId(userId);

        var normalizedContent =
            NormalizeContent(content);

        Id = Guid.NewGuid();
        TicketId = ticketId;
        UserId = userId;
        Content = normalizedContent;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Ticket Ticket { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }

    private static string NormalizeContent(
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Yorum içeriği boş olamaz.",
                nameof(content));
        }

        var normalizedContent = content.Trim();

        if (normalizedContent.Length >
            MaxContentLength)
        {
            throw new ArgumentException(
                $"Yorum içeriği en fazla " +
                $"{MaxContentLength} karakter olabilir.",
                nameof(content));
        }

        return normalizedContent;
    }

    private static void EnsureValidTicketId(
        Guid ticketId)
    {
        if (ticketId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir talep kimliği gereklidir.",
                nameof(ticketId));
        }
    }

    private static void EnsureValidUserId(
        Guid userId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kullanıcı kimliği gereklidir.",
                nameof(userId));
        }
    }
}