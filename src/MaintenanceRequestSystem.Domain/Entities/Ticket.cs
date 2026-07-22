using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class Ticket
{
    private Ticket()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public Ticket(
        Guid assetId,
        Guid createdByUserId,
        string title,
        string description,
        TicketPriority priority)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Talep başlığı boş olamaz.",
                nameof(title));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Talep açıklaması boş olamaz.",
                nameof(description));
        }

        Id = Guid.NewGuid();
        AssetId = assetId;
        CreatedByUserId = createdByUserId;
        Title = title.Trim();
        Description = description.Trim();
        Priority = priority;
        Status = TicketStatus.Open;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TicketPriority Priority { get; private set; }

    public TicketStatus Status { get; private set; }

    public Guid AssetId { get; private set; }

    public Asset Asset { get; private set; } = null!;

    public Guid CreatedByUserId { get; private set; }

    public User CreatedByUser { get; private set; } = null!;

    public Guid? AssignedTechnicianId { get; private set; }

    public User? AssignedTechnician { get; private set; }

    public string? WaitingReason { get; private set; }

    public string? ResolutionDescription { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? ResolvedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public ICollection<TicketComment> Comments { get; private set; }
        = new List<TicketComment>();

    public ICollection<TicketHistory> Histories { get; private set; }
        = new List<TicketHistory>();
}