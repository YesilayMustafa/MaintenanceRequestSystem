using MaintenanceRequestSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class User
{
    private User()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public User(
        string fullName,
        string email,
        string passwordHash,
        UserRole role,
        Guid departmentId)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Ad soyad boş olamaz.",
                nameof(fullName));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "E-posta adresi boş olamaz.",
                nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "Parola hash değeri boş olamaz.",
                nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        FullName = fullName.Trim();
        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        DepartmentId = departmentId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public UserRole Role { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Department Department { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public ICollection<Ticket> CreatedTickets { get; private set; }
        = new List<Ticket>();

    public ICollection<Ticket> AssignedTickets { get; private set; }
        = new List<Ticket>();

    public ICollection<TicketComment> Comments { get; private set; }
        = new List<TicketComment>();

    public ICollection<TicketHistory> TicketHistories { get; private set; }
        = new List<TicketHistory>();

    public ICollection<AuditLog> AuditLogs { get; private set; }
        = new List<AuditLog>();

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