using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class Department
{
    private Department()
    {
        // Entity Framework Core tarafından kullanılacak.
    }



    public Department(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Departman adı boş olamaz.",
                nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public ICollection<User> Users { get; private set; } = new List<User>();

    public ICollection<Asset> Assets { get; private set; } = new List<Asset>();

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Departman adı boş olamaz.",
                nameof(name));
        }

        Name = name.Trim();

        Description = string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();

        UpdatedAt = DateTime.UtcNow;
    }
}