using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Domain.Entities;

public sealed class AccountToken
{
    public const int MaxTokenHashLength = 64;

    private AccountToken()
    {
        // Entity Framework Core tarafından kullanılacak.
    }

    public AccountToken(
        Guid userId,
        string tokenHash,
        AccountTokenType type,
        DateTime expiresAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kullanıcı kimliği gereklidir.",
                nameof(userId));
        }

        if (!Enum.IsDefined(type))
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                "Geçersiz hesap token türü.");
        }

        if (expiresAt.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Token son kullanma tarihi UTC olmalıdır.",
                nameof(expiresAt));
        }

        var createdAt = DateTime.UtcNow;

        if (expiresAt <= createdAt)
        {
            throw new ArgumentException(
                "Token son kullanma tarihi gelecekte olmalıdır.",
                nameof(expiresAt));
        }

        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = NormalizeTokenHash(tokenHash);
        Type = type;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public User User { get; private set; } = null!;

    public string TokenHash { get; private set; } = string.Empty;

    public AccountTokenType Type { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    public DateTime? UsedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public bool CanBeUsed(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));

        return UsedAt is null &&
            RevokedAt is null &&
            ExpiresAt > utcNow;
    }

    public void Consume(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));

        if (!CanBeUsed(utcNow))
        {
            throw new InvalidOperationException(
                "Token süresi dolmuş, kullanılmış veya iptal edilmiş.");
        }

        UsedAt = utcNow;
    }

    public void Revoke(DateTime utcNow)
    {
        EnsureUtc(utcNow, nameof(utcNow));

        if (UsedAt.HasValue)
        {
            throw new InvalidOperationException(
                "Kullanılmış token iptal edilemez.");
        }

        if (RevokedAt.HasValue)
        {
            return;
        }

        RevokedAt = utcNow;
    }

    private static string NormalizeTokenHash(string tokenHash)
    {
        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException(
                "Token özeti boş olamaz.",
                nameof(tokenHash));
        }

        var normalizedTokenHash = tokenHash.Trim();

        if (normalizedTokenHash.Length > MaxTokenHashLength)
        {
            throw new ArgumentException(
                $"Token özeti en fazla {MaxTokenHashLength} karakter olabilir.",
                nameof(tokenHash));
        }

        return normalizedTokenHash;
    }

    private static void EnsureUtc(
        DateTime value,
        string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                "Token işlem tarihi UTC olmalıdır.",
                parameterName);
        }
    }
}
