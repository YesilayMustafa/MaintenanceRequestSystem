using MaintenanceRequestSystem.Infrastructure.Authentication;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class AccountTokenGeneratorTests
{
    [Fact]
    public void Generate_CreatesDistinctRawTokenAndDeterministicHash()
    {
        // Arrange
        var generator = new AccountTokenGenerator();

        // Act
        var generatedToken = generator.Generate();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(generatedToken.RawToken));
        Assert.False(string.IsNullOrWhiteSpace(generatedToken.TokenHash));
        Assert.NotEqual(
            generatedToken.RawToken,
            generatedToken.TokenHash);
        Assert.Equal(
            generatedToken.TokenHash,
            generator.HashToken(generatedToken.RawToken));
    }

    [Fact]
    public void Generate_WhenCalledTwice_CreatesDifferentTokens()
    {
        // Arrange
        var generator = new AccountTokenGenerator();

        // Act
        var first = generator.Generate();
        var second = generator.Generate();

        // Assert
        Assert.NotEqual(first.RawToken, second.RawToken);
        Assert.NotEqual(first.TokenHash, second.TokenHash);
    }
}
