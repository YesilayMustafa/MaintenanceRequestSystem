using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class TicketComment
{
    private TicketComment()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public TicketComment(
        Guid ticketId,
        Guid userId,
        string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException(
                "Yorum içeriği boş olamaz.",
                nameof(content));
        }

        Id = Guid.NewGuid();
        TicketId = ticketId;
        UserId = userId;
        Content = content.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Ticket Ticket { get; private set; } = null!;

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string Content { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
}