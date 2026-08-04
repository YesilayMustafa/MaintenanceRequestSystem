using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

/// <summary>
/// Bir bakım talebinin durumunu, atamasını ve değişiklik geçmişini yöneten domain varlığıdır.
/// </summary>
public sealed class Ticket
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 4000;
    public const int MaxWaitingReasonLength = 1000;
    public const int MaxResolutionDescriptionLength = 2000;

    private Ticket()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    /// <summary>
    /// Yeni bir talebi açık durumda oluşturur.
    /// </summary>
    public Ticket(
        Guid assetId,
        Guid createdByUserId,
        string title,
        string description,
        TicketPriority priority)
    {
        var normalizedTitle =
            NormalizeTitle(title);

        var normalizedDescription =
            NormalizeDescription(description);

        EnsureValidAssetId(assetId);
        EnsureValidUserId(createdByUserId);
        EnsureValidPriority(priority);

        Id = Guid.NewGuid();
        AssetId = assetId;
        CreatedByUserId = createdByUserId;
        Title = normalizedTitle;
        Description = normalizedDescription;
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

    /// <summary>
    /// Açık bir talebi ilk kez teknik personele atar ve Open → Assigned geçmişini oluşturur.
    /// Application katmanı bu davranışı yalnızca Admin adına ve aktif Technician hedefi için çağırmalıdır;
    /// sonraki atamalar <see cref="Reassign"/> ile yapılır.
    /// </summary>
    /// <remarks>
    /// Durum geçişi ile history kaydı aynı domain işleminin parçasıdır; böylece güncel durum ve audit izi ayrışmaz.
    /// </remarks>
    public void Assign(
        Guid technicianId,
        Guid performedByUserId)
    {
        EnsureValidTechnicianId(technicianId);
        EnsureValidPerformedByUserId(performedByUserId);

        // İlk atama yalnızca Open durumundan yapılabilir; atanmış talepler Reassign ile yönetilir.
        if (Status != TicketStatus.Open)
        {
            throw new ArgumentException(
                "Yalnızca açık durumdaki talepler atanabilir.");
        }

        var oldStatus = Status;

        // Durum geçişi ve history aynı aggregate işleminde tutularak audit izinin state ile uyumu korunur.
        AssignedTechnicianId = technicianId;
        Status = TicketStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                "Talep teknik personele atandı."));
    }

    /// <summary>
    /// Atanmış bir talebin teknisyenini değiştirir ve atanan kişi değişikliğini history kaydına ekler.
    /// Application katmanı bu davranışı yalnızca Admin adına ve farklı, aktif bir Technician hedefi için çağırmalıdır.
    /// </summary>
    /// <remarks>
    /// İlk atamadan farklı olarak durum Assigned kalır; atama değişikliği ile history aynı işlemde kaydedilir.
    /// </remarks>
    public void Reassign(
    Guid technicianId,
    Guid performedByUserId)
    {
        EnsureValidTechnicianId(technicianId);
        EnsureValidPerformedByUserId(performedByUserId);

        // Yeniden atama yalnızca ilk ataması tamamlanmış taleplerde anlamlıdır.
        if (Status != TicketStatus.Assigned)
        {
            throw new ArgumentException(
                "Yalnızca atanmış durumdaki talepler yeniden atanabilir.");
        }

        if (AssignedTechnicianId == technicianId)
        {
            throw new ArgumentException(
                "Talep zaten bu teknik personele atanmış.");
        }

        var oldStatus = Status;

        AssignedTechnicianId = technicianId;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                "Talep başka bir teknik personele yeniden atandı."));
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException(
                "Talep başlığı boş olamaz.",
                nameof(title));
        }

        var normalizedTitle = title.Trim();

        if (normalizedTitle.Length > MaxTitleLength)
        {
            throw new ArgumentException(
                $"Talep başlığı en fazla " +
                $"{MaxTitleLength} karakter olabilir.",
                nameof(title));
        }

        return normalizedTitle;
    }

    private static string NormalizeDescription(
        string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException(
                "Talep açıklaması boş olamaz.",
                nameof(description));
        }

        var normalizedDescription =
            description.Trim();

        if (normalizedDescription.Length >
            MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Talep açıklaması en fazla " +
                $"{MaxDescriptionLength} karakter olabilir.",
                nameof(description));
        }

        return normalizedDescription;
    }

    private static void EnsureValidAssetId(Guid assetId)
    {
        if (assetId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir cihaz kimliği gereklidir.",
                nameof(assetId));
        }
    }

    private static void EnsureValidUserId(
        Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kullanıcı kimliği gereklidir.",
                nameof(createdByUserId));
        }
    }

    private static void EnsureValidPriority(
        TicketPriority priority)
    {
        if (!Enum.IsDefined(priority))
        {
            throw new ArgumentOutOfRangeException(
                nameof(priority),
                "Geçersiz talep önceliği.");
        }
    }

    private static void EnsureValidTechnicianId(
        Guid technicianId)
    {
        if (technicianId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir teknik personel kimliği gereklidir.",
                nameof(technicianId));
        }
    }

    private static void EnsureValidPerformedByUserId(
        Guid performedByUserId)
    {
        if (performedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir işlemi yapan kullanıcı kimliği gereklidir.",
                nameof(performedByUserId));
        }
    }
}
