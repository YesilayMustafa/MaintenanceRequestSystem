using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class AuditLog
{
    private AuditLog()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public AuditLog(
        Guid performedByUserId,
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException(
                "Audit işlem adı boş olamaz.",
                nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException(
                "Entity adı boş olamaz.",
                nameof(entityName));
        }

        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentException(
                "Entity kimliği boş olamaz.",
                nameof(entityId));
        }

        Id = Guid.NewGuid();
        PerformedByUserId = performedByUserId;
        Action = action.Trim();
        EntityName = entityName.Trim();
        EntityId = entityId.Trim();
        OldValues = oldValues;
        NewValues = newValues;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public User PerformedByUser { get; private set; } = null!;

    public string Action { get; private set; } = string.Empty;

    public string EntityName { get; private set; } = string.Empty;

    public string EntityId { get; private set; } = string.Empty;

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public DateTime CreatedAt { get; private set; }
}