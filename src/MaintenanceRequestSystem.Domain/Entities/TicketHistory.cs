using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class TicketHistory
{
    private TicketHistory()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public TicketHistory(
        Guid ticketId,
        Guid performedByUserId,
        TicketStatus? oldStatus,
        TicketStatus newStatus,
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Geçmiş açıklaması boş olamaz.",
                nameof(description));
        }

        Id = Guid.NewGuid();
        TicketId = ticketId;
        PerformedByUserId = performedByUserId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Description = description.Trim();
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid TicketId { get; private set; }

    public Ticket Ticket { get; private set; } = null!;

    public Guid PerformedByUserId { get; private set; }

    public User PerformedByUser { get; private set; } = null!;

    public TicketStatus? OldStatus { get; private set; }

    public TicketStatus NewStatus { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public DateTime CreatedAt { get; private set; }
}