using MaintenanceRequestSystem.Domain.Enums;

using MaintenanceRequestSystem.Domain.ValueObjects;

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
    public const int MaxReopenReasonLength = 1000;

    private Ticket()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    /// <summary>
    /// Yeni bir talebi açık durumda oluşturur.
    /// </summary>
    public Ticket(
        string ticketNumber,
        Guid assetId,
        Guid createdByUserId,
        string title,
        string description,
        TicketPriority priority)
        : this(
            ticketNumber,
            assetId,
            TicketCategory.OtherId,
            createdByUserId,
            title,
            description,
            priority)
    {
    }

    public Ticket(
        string ticketNumber,
        Guid assetId,
        Guid categoryId,
        Guid createdByUserId,
        string title,
        string description,
        TicketPriority priority)
    {
        var normalizedTitle =
            NormalizeTitle(title);

        var normalizedDescription =
            NormalizeDescription(description);

        var normalizedTicketNumber =
            TicketNumberValue.Normalize(ticketNumber);

        EnsureValidAssetId(assetId);
        EnsureValidCategoryId(categoryId);
        EnsureValidUserId(createdByUserId);
        EnsureValidPriority(priority);

        Id = Guid.NewGuid();
        TicketNumber = normalizedTicketNumber;
        AssetId = assetId;
        CategoryId = categoryId;
        CreatedByUserId = createdByUserId;
        Title = normalizedTitle;
        Description = normalizedDescription;
        Priority = priority;
        Status = TicketStatus.Open;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string TicketNumber { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TicketPriority Priority { get; private set; }

    public TicketStatus Status { get; private set; }

    public Guid AssetId { get; private set; }

    public Asset Asset { get; private set; } = null!;

    public Guid CategoryId { get; private set; }

    public TicketCategory Category { get; private set; } = null!;

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

    /// <summary>
    /// Talebin soft delete ile pasifleştirilip pasifleştirilmediğini belirtir.
    /// </summary>
    public bool IsDeleted { get; private set; }

    /// <summary>
    /// Talebin pasifleştirildiği UTC zamanı tutar.
    /// </summary>
    public DateTime? DeletedAt { get; private set; }

    /// <summary>
    /// Talebi pasifleştiren kullanıcının kimliğini tutar.
    /// </summary>
    public Guid? DeletedByUserId { get; private set; }

    public ICollection<TicketComment> Comments { get; private set; }
        = new List<TicketComment>();

    public ICollection<TicketHistory> Histories { get; private set; }
        = new List<TicketHistory>();

    public ICollection<TicketAttachment> Attachments { get; private set; }
        = new List<TicketAttachment>();

    public ICollection<Notification> Notifications { get; private set; }
        = new List<Notification>();

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

    public void StartProgress(
    Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status != TicketStatus.Assigned)
        {
            throw new ArgumentException(
                "Yalnızca atanmış durumdaki talepler işleme alınabilir.");
        }

        if (AssignedTechnicianId != performedByUserId)
        {
            throw new ArgumentException(
                "Talebi yalnızca atanmış teknik personel işleme alabilir.");
        }

        var oldStatus = Status;

        Status = TicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                "Teknik personel talebi işleme aldı."));
    }

    /// <summary>
    /// İşlemdeki talebi, bekleme nedeni zorunlu olacak şekilde
    /// Waiting durumuna geçirir ve durum geçmişi oluşturur.
    /// </summary>
    public void PutOnHold(
        string reason,
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status != TicketStatus.InProgress)
        {
            throw new ArgumentException(
                "Yalnızca işlemdeki talepler beklemeye alınabilir.");
        }

        if (AssignedTechnicianId != performedByUserId)
        {
            throw new ArgumentException(
                "Talebi yalnızca atanmış teknik personel beklemeye alabilir.");
        }

        var normalizedReason =
            NormalizeWaitingReason(reason);

        var oldStatus = Status;

        WaitingReason = normalizedReason;
        Status = TicketStatus.Waiting;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                $"Talep beklemeye alındı: {normalizedReason}"));
    }

    /// <summary>
    /// Beklemedeki talebi tekrar işleme alır, bekleme nedenini temizler
    /// ve Waiting → InProgress geçmişini oluşturur.
    /// </summary>
    public void Resume(
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status != TicketStatus.Waiting)
        {
            throw new ArgumentException(
                "Yalnızca beklemedeki taleplerde işleme devam edilebilir.");
        }

        if (AssignedTechnicianId != performedByUserId)
        {
            throw new ArgumentException(
                "Talepte yalnızca atanmış teknik personel işleme devam edebilir.");
        }

        var oldStatus = Status;

        WaitingReason = null;
        Status = TicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                "Teknik personel talepte işleme devam etti."));
    }

    /// <summary>
    /// İşlemdeki talebi çözüm açıklamasıyla Resolved durumuna geçirir
    /// ve durum geçmişini oluşturur.
    /// </summary>
    public void Resolve(
        string resolutionDescription,
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status != TicketStatus.InProgress)
        {
            throw new ArgumentException(
                "Yalnızca işlemdeki talepler çözülebilir.");
        }

        if (AssignedTechnicianId != performedByUserId)
        {
            throw new ArgumentException(
                "Talebi yalnızca atanmış teknik personel çözebilir.");
        }

        var normalizedDescription =
            NormalizeResolutionDescription(
                resolutionDescription);

        var oldStatus = Status;
        var now = DateTime.UtcNow;

        ResolutionDescription =
            normalizedDescription;

        Status = TicketStatus.Resolved;
        ResolvedAt = now;
        UpdatedAt = now;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                $"Talep çözüldü: {normalizedDescription}"));
    }

    /// <summary>
    /// Çözümlenmiş talebi Closed durumuna geçirir, kapanış zamanını
    /// günceller ve durum geçmişini oluşturur.
    /// </summary>
    public void Close(
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status != TicketStatus.Resolved)
        {
            throw new ArgumentException(
                "Yalnızca çözümlenmiş talepler kapatılabilir.");
        }

        var oldStatus = Status;
        var now = DateTime.UtcNow;

        Status = TicketStatus.Closed;
        ClosedAt = now;
        UpdatedAt = now;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                "Talep kapatıldı."));
    }

    /// <summary>
    /// Kapatılmış talebi, yeniden açma nedeni zorunlu olacak şekilde
    /// InProgress durumuna geçirir ve önceki çözüm bilgilerini temizler.
    /// </summary>
    public void Reopen(
        string reason,
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status != TicketStatus.Closed)
        {
            throw new ArgumentException(
                "Yalnızca kapatılmış talepler yeniden açılabilir.");
        }

        var normalizedReason =
            NormalizeReopenReason(reason);

        var oldStatus = Status;
        var now = DateTime.UtcNow;

        Status = TicketStatus.InProgress;

        ResolutionDescription = null;
        ResolvedAt = null;
        ClosedAt = null;
        WaitingReason = null;

        UpdatedAt = now;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                $"Talep yeniden açıldı: {normalizedReason}"));
    }

    /// <summary>
    /// İptale uygun durumdaki talebi Cancelled durumuna geçirir
    /// ve durum geçmişini oluşturur.
    /// </summary>
    public void Cancel(
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (Status is not
            (TicketStatus.Open or
             TicketStatus.Assigned or
             TicketStatus.Waiting))
        {
            throw new ArgumentException(
                "Yalnızca açık, atanmış veya beklemedeki talepler iptal edilebilir.");
        }

        var oldStatus = Status;

        Status = TicketStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                oldStatus,
                Status,
                "Talep iptal edildi."));
    }



    /// <summary>
    /// Aktif durumdaki talebin önceliğini değiştirir.
    /// Tamamlanmış veya iptal edilmiş taleplerin önceliği değiştirilemez.
    /// </summary>
    public void ChangePriority(
        TicketPriority newPriority,
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (!Enum.IsDefined(
                typeof(TicketPriority),
                newPriority))
        {
            throw new ArgumentException(
                "Geçerli bir talep önceliği gereklidir.",
                nameof(newPriority));
        }

        if (Status is
            TicketStatus.Resolved or
            TicketStatus.Closed or
            TicketStatus.Cancelled)
        {
            throw new ArgumentException(
                "Tamamlanmış veya iptal edilmiş taleplerin önceliği değiştirilemez.");
        }

        if (Priority == newPriority)
        {
            throw new ArgumentException(
                "Talep zaten belirtilen önceliğe sahiptir.",
                nameof(newPriority));
        }

        Priority = newPriority;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangeCategory(
        Guid categoryId,
        Guid performedByUserId,
        string oldCategoryName,
        string newCategoryName)
    {
        EnsureValidCategoryId(categoryId);
        EnsureValidPerformedByUserId(performedByUserId);

        if (CategoryId == categoryId)
        {
            return;
        }

        var normalizedOldName = NormalizeCategoryName(
            oldCategoryName,
            nameof(oldCategoryName));

        var normalizedNewName = NormalizeCategoryName(
            newCategoryName,
            nameof(newCategoryName));

        CategoryId = categoryId;
        UpdatedAt = DateTime.UtcNow;

        Histories.Add(
            new TicketHistory(
                Id,
                performedByUserId,
                Status,
                Status,
                $"Talep kategorisi '{normalizedOldName}' değerinden " +
                $"'{normalizedNewName}' değerine değiştirildi."));
    }

    /// <summary>
    /// Tamamlanmış veya iptal edilmiş talebi fiziksel olarak silmeden
    /// pasifleştirir ve silme bilgilerini kaydeder.
    /// </summary>
    public void SoftDelete(
        Guid performedByUserId)
    {
        EnsureValidPerformedByUserId(
            performedByUserId);

        if (IsDeleted)
        {
            throw new ArgumentException(
                "Talep zaten pasifleştirilmiştir.");
        }

        if (Status is not
            (TicketStatus.Closed or
             TicketStatus.Cancelled))
        {
            throw new ArgumentException(
                "Yalnızca kapatılmış veya iptal edilmiş talepler pasifleştirilebilir.");
        }

        var now = DateTime.UtcNow;

        IsDeleted = true;
        DeletedAt = now;
        DeletedByUserId = performedByUserId;
        UpdatedAt = now;
    }

    private static string NormalizeReopenReason(
    string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Yeniden açma nedeni gereklidir.",
                nameof(reason));
        }

        var normalizedReason =
            reason.Trim();

        if (normalizedReason.Length >
            MaxReopenReasonLength)
        {
            throw new ArgumentException(
                $"Yeniden açma nedeni en fazla {MaxReopenReasonLength} karakter olabilir.",
                nameof(reason));
        }

        return normalizedReason;
    }



    private static string NormalizeResolutionDescription(
    string resolutionDescription)
    {
        if (string.IsNullOrWhiteSpace(
                resolutionDescription))
        {
            throw new ArgumentException(
                "Çözüm açıklaması gereklidir.",
                nameof(resolutionDescription));
        }

        var normalizedDescription =
            resolutionDescription.Trim();

        if (normalizedDescription.Length >
            MaxResolutionDescriptionLength)
        {
            throw new ArgumentException(
                $"Çözüm açıklaması en fazla {MaxResolutionDescriptionLength} karakter olabilir.",
                nameof(resolutionDescription));
        }

        return normalizedDescription;
    }

    private static string NormalizeWaitingReason(
    string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "Bekleme nedeni gereklidir.",
                nameof(reason));
        }

        var normalizedReason =
            reason.Trim();

        if (normalizedReason.Length >
            MaxWaitingReasonLength)
        {
            throw new ArgumentException(
                $"Bekleme nedeni en fazla {MaxWaitingReasonLength} karakter olabilir.",
                nameof(reason));
        }

        return normalizedReason;
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

    private static void EnsureValidCategoryId(Guid categoryId)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kategori kimliği gereklidir.",
                nameof(categoryId));
        }
    }

    private static string NormalizeCategoryName(
        string categoryName,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            throw new ArgumentException(
                "Kategori adı boş olamaz.",
                parameterName);
        }

        return categoryName.Trim();
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
