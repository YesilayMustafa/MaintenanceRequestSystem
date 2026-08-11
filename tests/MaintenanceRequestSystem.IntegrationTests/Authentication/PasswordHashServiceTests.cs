using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class PasswordHashServiceTests
{
    [Fact]
    public void HashPassword_ThenVerifyCorrectPassword_ReturnsTrue()
    {
        // Arrange
        var service = new PasswordHashService();
        const string password = "TestPassword123!";

        // Act
        var passwordHash =
            service.HashPassword(password);

        var result =
            service.VerifyPassword(
                passwordHash,
                password);

        // Assert
        Assert.NotEqual(password, passwordHash);
        Assert.True(result.Succeeded);
        Assert.False(result.NeedsRehash);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ReturnsFalse()
    {
        // Arrange
        var service = new PasswordHashService();

        var passwordHash =
            service.HashPassword("CorrectPassword123!");

        // Act
        var result =
            service.VerifyPassword(
                passwordHash,
                "WrongPassword123!");

        // Assert
        Assert.False(result.Succeeded);
        Assert.False(result.NeedsRehash);
    }

    [Fact]
    public void VerifyPassword_WithIdentityV2Hash_RequestsRehash()
    {
        // Arrange
        const string password = "LegacyPassword123!";

        var legacyHasher = new PasswordHasher<object>(
            Options.Create(
                new PasswordHasherOptions
                {
                    CompatibilityMode =
                        PasswordHasherCompatibilityMode.IdentityV2
                }));

        var legacyHash = legacyHasher.HashPassword(
            new object(),
            password);

        var service = new PasswordHashService();

        // Act
        var result = service.VerifyPassword(
            legacyHash,
            password);

        // Assert
        Assert.True(result.Succeeded);
        Assert.True(result.NeedsRehash);
    }
}
