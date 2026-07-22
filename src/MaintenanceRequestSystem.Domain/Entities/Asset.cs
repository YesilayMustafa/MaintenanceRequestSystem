using MaintenanceRequestSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class Asset
{
    private Asset()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public Asset(
        string name,
        string serialNumber,
        AssetType type,
        Guid departmentId,
        string? location = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Cihaz adı boş olamaz.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new ArgumentException(
                "Seri numarası boş olamaz.",
                nameof(serialNumber));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        SerialNumber = serialNumber.Trim().ToUpperInvariant();
        Type = type;
        DepartmentId = departmentId;
        Location = location?.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string SerialNumber { get; private set; } = string.Empty;

    public AssetType Type { get; private set; }

    public string? Location { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Department Department { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public ICollection<Ticket> Tickets { get; private set; }
        = new List<Ticket>();

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }
}