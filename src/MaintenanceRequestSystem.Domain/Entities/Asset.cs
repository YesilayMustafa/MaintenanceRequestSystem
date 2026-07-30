using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class Asset
{
    public const int MaxNameLength = 150;
    public const int MaxSerialNumberLength = 100;
    public const int MaxLocationLength = 200;

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
        var normalizedName =
            NormalizeName(name);

        var normalizedSerialNumber =
            NormalizeSerialNumber(serialNumber);

        var normalizedLocation =
            NormalizeLocation(location);

        EnsureValidType(type);
        EnsureValidDepartmentId(departmentId);

        Id = Guid.NewGuid();
        Name = normalizedName;
        SerialNumber = normalizedSerialNumber;
        Type = type;
        DepartmentId = departmentId;
        Location = normalizedLocation;
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

    public void UpdateDetails(
        string name,
        string serialNumber,
        AssetType type,
        Guid departmentId,
        string? location)
    {
        var normalizedName =
            NormalizeName(name);

        var normalizedSerialNumber =
            NormalizeSerialNumber(serialNumber);

        var normalizedLocation =
            NormalizeLocation(location);

        EnsureValidType(type);
        EnsureValidDepartmentId(departmentId);

        Name = normalizedName;
        SerialNumber = normalizedSerialNumber;
        Type = type;
        DepartmentId = departmentId;
        Location = normalizedLocation;
        UpdatedAt = DateTime.UtcNow;
    }

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

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Cihaz adı boş olamaz.",
                nameof(name));
        }

        var normalizedName = name.Trim();

        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException(
                $"Cihaz adı en fazla {MaxNameLength} karakter olabilir.",
                nameof(name));
        }

        return normalizedName;
    }

    private static string NormalizeSerialNumber(
        string serialNumber)
    {
        if (string.IsNullOrWhiteSpace(serialNumber))
        {
            throw new ArgumentException(
                "Seri numarası boş olamaz.",
                nameof(serialNumber));
        }

        var normalizedSerialNumber =
            serialNumber.Trim().ToUpperInvariant();

        if (normalizedSerialNumber.Length >
            MaxSerialNumberLength)
        {
            throw new ArgumentException(
                $"Seri numarası en fazla " +
                $"{MaxSerialNumberLength} karakter olabilir.",
                nameof(serialNumber));
        }

        return normalizedSerialNumber;
    }

    private static string? NormalizeLocation(
        string? location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            return null;
        }

        var normalizedLocation = location.Trim();

        if (normalizedLocation.Length >
            MaxLocationLength)
        {
            throw new ArgumentException(
                $"Konum en fazla {MaxLocationLength} karakter olabilir.",
                nameof(location));
        }

        return normalizedLocation;
    }

    private static void EnsureValidType(AssetType type)
    {
        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Geçersiz cihaz türü.");
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
}