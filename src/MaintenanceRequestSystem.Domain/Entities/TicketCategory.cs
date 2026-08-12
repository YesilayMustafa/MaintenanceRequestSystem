using System.Globalization;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class TicketCategory
{
    public const int MaxNameLength = 100;
    public const int MaxNormalizedNameLength = 100;
    public const int MaxDescriptionLength = 500;

    public static readonly Guid OtherId =
        Guid.Parse("10000000-0000-0000-0000-000000000006");

    private static readonly CultureInfo TurkishCulture =
        CultureInfo.GetCultureInfo("tr-TR");

    private TicketCategory()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public TicketCategory(
        string name,
        string? description = null)
        : this(Guid.NewGuid(), name, description)
    {
    }

    public TicketCategory(
        Guid id,
        string name,
        string? description = null)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kategori kimliği gereklidir.",
                nameof(id));
        }

        var normalizedDisplayName = NormalizeDisplayName(name);

        Id = id;
        Name = normalizedDisplayName;
        NormalizedName = NormalizeName(normalizedDisplayName);
        Description = NormalizeDescription(description);
        IsActive = true;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public ICollection<Ticket> Tickets { get; private set; } = [];

    public void UpdateDetails(string name, string? description)
    {
        var normalizedDisplayName = NormalizeDisplayName(name);

        Name = normalizedDisplayName;
        NormalizedName = NormalizeName(normalizedDisplayName);
        Description = NormalizeDescription(description);
        UpdatedAt = DateTime.UtcNow;
    }

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

    public static string NormalizeName(string name)
    {
        return NormalizeDisplayName(name)
            .Normalize(NormalizationForm.FormKC)
            .ToUpper(TurkishCulture);
    }

    private static string NormalizeDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Kategori adı boş olamaz.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Kategori adı en fazla {MaxNameLength} karakter olabilir.",
                nameof(name));
        }

        return normalizedName;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var normalizedDescription = description.Trim();

        if (normalizedDescription.Length > MaxDescriptionLength)
        {
            throw new ArgumentException(
                $"Kategori açıklaması en fazla {MaxDescriptionLength} karakter olabilir.",
                nameof(description));
        }

        return normalizedDescription;
    }
}
