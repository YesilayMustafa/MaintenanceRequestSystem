using System.Text.Json;

namespace MaintenanceRequestSystem.Domain.Entities;

/// <summary>
/// Sistemde gerçekleştirilen kritik bir işlemin
/// denetim kaydını temsil eder.
/// </summary>
public sealed class AuditLog
{
    public const int MaxActionLength = 100;
    public const int MaxEntityNameLength = 100;
    public const int MaxEntityIdLength = 100;

    private AuditLog()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    /// <summary>
    /// Yeni bir audit kaydı oluşturur.
    /// OldValues ve NewValues alanları geçerli JSON olmalıdır.
    /// </summary>
    public AuditLog(
        Guid performedByUserId,
        string action,
        string entityName,
        string entityId,
        string? oldValues = null,
        string? newValues = null)
    {
        if (performedByUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "İşlemi yapan kullanıcı kimliği boş olamaz.",
                nameof(performedByUserId));
        }

        Id = Guid.NewGuid();
        PerformedByUserId = performedByUserId;

        Action = NormalizeRequiredValue(
            action,
            MaxActionLength,
            nameof(action),
            "Audit işlem adı");

        EntityName = NormalizeRequiredValue(
            entityName,
            MaxEntityNameLength,
            nameof(entityName),
            "Entity adı");

        EntityId = NormalizeRequiredValue(
            entityId,
            MaxEntityIdLength,
            nameof(entityId),
            "Entity kimliği");

        OldValues = NormalizeJson(
            oldValues,
            nameof(oldValues));

        NewValues = NormalizeJson(
            newValues,
            nameof(newValues));

        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PerformedByUserId { get; private set; }

    public User PerformedByUser { get; private set; } = null!;

    public string Action { get; private set; } =
        string.Empty;

    public string EntityName { get; private set; } =
        string.Empty;

    public string EntityId { get; private set; } =
        string.Empty;

    public string? OldValues { get; private set; }

    public string? NewValues { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private static string NormalizeRequiredValue(
        string value,
        int maxLength,
        string parameterName,
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(
                $"{displayName} boş olamaz.",
                parameterName);
        }

        var normalizedValue =
            value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new ArgumentException(
                $"{displayName} en fazla {maxLength} karakter olabilir.",
                parameterName);
        }

        return normalizedValue;
    }

    private static string? NormalizeJson(
        string? value,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedValue =
            value.Trim();

        try
        {
            using var document =
                JsonDocument.Parse(normalizedValue);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException(
                "Audit değerleri geçerli bir JSON olmalıdır.",
                parameterName,
                exception);
        }

        return normalizedValue;
    }
}
