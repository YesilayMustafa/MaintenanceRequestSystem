using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class AccountTokenPersistenceTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AccountTokenPersistenceTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.CreateClient();
    }

    [Fact]
    public async Task AccountTokenRepository_PersistsOnlyTokenHash()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var repository = scope.ServiceProvider
            .GetRequiredService<IAccountTokenRepository>();

        var generator = scope.ServiceProvider
            .GetRequiredService<IAccountTokenGenerator>();

        var userId = await context.Users
            .Select(user => user.Id)
            .FirstAsync();

        var generatedToken = generator.Generate();

        var accountToken = new AccountToken(
            userId,
            generatedToken.TokenHash,
            AccountTokenType.Invitation,
            DateTime.UtcNow.AddHours(1));

        await repository.AddAsync(accountToken);
        await repository.SaveChangesAsync();
        context.ChangeTracker.Clear();

        // Act
        var persistedToken = await repository.GetByHashAsync(
            generator.HashToken(generatedToken.RawToken));

        // Assert
        Assert.NotNull(persistedToken);
        Assert.Equal(generatedToken.TokenHash, persistedToken.TokenHash);
        Assert.NotEqual(generatedToken.RawToken, persistedToken.TokenHash);
    }

    [Fact]
    public void AccountTokenModel_HasNoRawTokenPropertyAndUsesUniqueHashIndex()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var entityType = context.Model.FindEntityType(
            typeof(AccountToken));

        // Act
        var rawTokenProperty = entityType?.FindProperty("RawToken");

        var tokenHashIndex = entityType?.GetIndexes()
            .SingleOrDefault(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name ==
                    nameof(AccountToken.TokenHash));

        // Assert
        Assert.Null(rawTokenProperty);
        Assert.NotNull(tokenHashIndex);
        Assert.True(tokenHashIndex.IsUnique);
    }
}
