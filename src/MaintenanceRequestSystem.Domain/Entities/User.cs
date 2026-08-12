using System.Net.Mail;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class User
{
    public const int MaxFullNameLength = 150;
    public const int MaxEmailLength = 255;
    public const int MaxPasswordHashLength = 500;

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
        Id = Guid.NewGuid();

        FullName = NormalizeFullName(fullName);
        Email = NormalizeEmail(email);
        PasswordHash = NormalizePasswordHash(passwordHash);

        EnsureValidRole(role);
        EnsureValidDepartmentId(departmentId);

        Role = role;
        DepartmentId = departmentId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        InvitationAcceptedAt = CreatedAt;
        SecurityVersion = 1;
    }

    private User(
        string fullName,
        string email,
        UserRole role,
        Guid departmentId)
    {
        Id = Guid.NewGuid();

        FullName = NormalizeFullName(fullName);
        Email = NormalizeEmail(email);

        EnsureValidRole(role);
        EnsureValidDepartmentId(departmentId);

        Role = role;
        DepartmentId = departmentId;
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
        SecurityVersion = 1;
    }

    public Guid Id { get; private set; }

    public string FullName { get; private set; } = string.Empty;

    public string Email { get; private set; } = string.Empty;

    public string? PasswordHash { get; private set; }

    public UserRole Role { get; private set; }

    public Guid DepartmentId { get; private set; }

    public Department Department { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public DateTime? InvitationAcceptedAt { get; private set; }

    public int SecurityVersion { get; private set; }

    public bool IsOperational =>
        IsActive &&
        InvitationAcceptedAt.HasValue &&
        !string.IsNullOrWhiteSpace(PasswordHash);

    public AccountStatus AccountStatus =>
        !IsActive
            ? AccountStatus.Inactive
            : IsOperational
                ? AccountStatus.Active
                : AccountStatus.PendingInvitation;

    public ICollection<Ticket> CreatedTickets { get; private set; }
        = new List<Ticket>();

    public ICollection<Ticket> AssignedTickets { get; private set; }
        = new List<Ticket>();

    public ICollection<TicketComment> Comments { get; private set; }
        = new List<TicketComment>();

    public ICollection<TicketHistory> TicketHistories { get; private set; }
        = new List<TicketHistory>();

    public ICollection<TicketAttachment> UploadedAttachments { get; private set; }
        = new List<TicketAttachment>();

    public ICollection<AuditLog> AuditLogs { get; private set; }
        = new List<AuditLog>();

    public ICollection<Notification> Notifications { get; private set; }
        = new List<Notification>();

    public ICollection<AccountToken> AccountTokens { get; private set; }
        = new List<AccountToken>();

    public static User CreateInvited(
        string fullName,
        string email,
        UserRole role,
        Guid departmentId)
    {
        return new User(
            fullName,
            email,
            role,
            departmentId);
    }

    public void AcceptInvitation(string passwordHash)
    {
        if (InvitationAcceptedAt.HasValue)
        {
            throw new InvalidOperationException(
                "Kullanıcı daveti daha önce kabul edilmiş.");
        }

        if (!IsActive)
        {
            throw new InvalidOperationException(
                "Pasif kullanıcı daveti kabul edemez.");
        }

        PasswordHash = NormalizePasswordHash(passwordHash);
        InvitationAcceptedAt = DateTime.UtcNow;
        IncrementSecurityVersion();
        UpdatedAt = InvitationAcceptedAt;
    }

    public void ChangePasswordHash(string passwordHash)
    {
        if (!InvitationAcceptedAt.HasValue)
        {
            throw new InvalidOperationException(
                "Daveti kabul edilmemiş kullanıcının parolası değiştirilemez.");
        }

        PasswordHash = NormalizePasswordHash(passwordHash);
        IncrementSecurityVersion();
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(
        string fullName,
        string email,
        Guid departmentId)
    {
        var normalizedFullName =
            NormalizeFullName(fullName);

        var normalizedEmail =
            NormalizeEmail(email);

        EnsureValidDepartmentId(departmentId);

        FullName = normalizedFullName;
        Email = normalizedEmail;
        DepartmentId = departmentId;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeRole(UserRole role)
    {
        EnsureValidRole(role);

        if (Role == role)
        {
            return;
        }

        Role = role;
        IncrementSecurityVersion();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (IsActive)
        {
            return;
        }

        IsActive = true;
        IncrementSecurityVersion();
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        if (!IsActive)
        {
            return;
        }

        IsActive = false;
        IncrementSecurityVersion();
        UpdatedAt = DateTime.UtcNow;
    }

    private static string NormalizeFullName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new ArgumentException(
                "Kullanıcı adı ve soyadı boş olamaz.",
                nameof(fullName));
        }

        var normalizedFullName = fullName.Trim();

        if (normalizedFullName.Length > MaxFullNameLength)
        {
            throw new ArgumentException(
                $"Kullanıcı adı ve soyadı en fazla " +
                $"{MaxFullNameLength} karakter olabilir.",
                nameof(fullName));
        }

        return normalizedFullName;
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException(
                "E-posta adresi boş olamaz.",
                nameof(email));
        }

        var normalizedEmail =
            email.Trim().ToLowerInvariant();

        if (normalizedEmail.Length > MaxEmailLength)
        {
            throw new ArgumentException(
                $"E-posta adresi en fazla " +
                $"{MaxEmailLength} karakter olabilir.",
                nameof(email));
        }

        if (!MailAddress.TryCreate(
                normalizedEmail,
                out var parsedEmail) ||
            !string.Equals(
                parsedEmail.Address,
                normalizedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Geçerli bir e-posta adresi girilmelidir.",
                nameof(email));
        }

        return normalizedEmail;
    }

    private static string NormalizePasswordHash(
        string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException(
                "Parola özeti boş olamaz.",
                nameof(passwordHash));
        }

        var normalizedPasswordHash =
            passwordHash.Trim();

        if (normalizedPasswordHash.Length >
            MaxPasswordHashLength)
        {
            throw new ArgumentException(
                $"Parola özeti en fazla " +
                $"{MaxPasswordHashLength} karakter olabilir.",
                nameof(passwordHash));
        }

        return normalizedPasswordHash;
    }

    private static void EnsureValidRole(UserRole role)
    {
        if (!Enum.IsDefined(role))
        {
            throw new ArgumentOutOfRangeException(
                nameof(role),
                "Geçersiz kullanıcı rolü.");
        }
    }

    private static void EnsureValidDepartmentId(
        Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir departman kimliği gereklidir.",
                nameof(departmentId));
        }
    }

    private void IncrementSecurityVersion()
    {
        SecurityVersion = checked(SecurityVersion + 1);
    }
}
