using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain.Entities;

public sealed class AccountTokenTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesUnusedToken()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var token = new AccountToken(
            userId,
            "token-hash",
            AccountTokenType.Invitation,
            expiresAt);

        // Assert
        Assert.NotEqual(Guid.Empty, token.Id);
        Assert.Equal(userId, token.UserId);
        Assert.Equal("token-hash", token.TokenHash);
        Assert.Equal(AccountTokenType.Invitation, token.Type);
        Assert.Equal(expiresAt, token.ExpiresAt);
        Assert.Null(token.UsedAt);
        Assert.Null(token.RevokedAt);
    }

    [Fact]
    public void Consume_WhenTokenIsValid_MarksTokenAsUsed()
    {
        // Arrange
        var token = CreateToken();
        var usedAt = DateTime.UtcNow;

        // Act
        token.Consume(usedAt);

        // Assert
        Assert.Equal(usedAt, token.UsedAt);
        Assert.False(token.CanBeUsed(usedAt));
    }

    [Fact]
    public void Consume_WhenTokenIsExpired_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = CreateToken();

        // Act
        var action = () =>
            token.Consume(token.ExpiresAt);

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Null(token.UsedAt);
    }

    [Fact]
    public void Consume_WhenTokenWasAlreadyUsed_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = CreateToken();
        var usedAt = DateTime.UtcNow;
        token.Consume(usedAt);

        // Act
        var action = () =>
            token.Consume(usedAt.AddSeconds(1));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
    }

    [Fact]
    public void Consume_WhenTokenWasRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = CreateToken();
        var revokedAt = DateTime.UtcNow;
        token.Revoke(revokedAt);

        // Act
        var action = () =>
            token.Consume(revokedAt.AddSeconds(1));

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Null(token.UsedAt);
    }

    private static AccountToken CreateToken()
    {
        return new AccountToken(
            Guid.NewGuid(),
            "token-hash",
            AccountTokenType.PasswordReset,
            DateTime.UtcNow.AddHours(1));
    }
}
