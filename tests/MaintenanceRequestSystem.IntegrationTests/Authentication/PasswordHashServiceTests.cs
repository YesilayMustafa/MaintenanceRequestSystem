using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Infrastructure.Authentication;

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
        Assert.True(result);
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
        Assert.False(result);
    }
}