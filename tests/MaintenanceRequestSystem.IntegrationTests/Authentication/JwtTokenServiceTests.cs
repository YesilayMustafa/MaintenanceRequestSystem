using System;
using System.Collections.Generic;
using System.Text;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_WithValidUser_ReturnsReadableToken()
    {
        // Arrange
        var signingKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(32));

        var options = Options.Create(
            new JwtOptions
            {
                Issuer = "TestIssuer",
                Audience = "TestAudience",
                SigningKey = signingKey,
                ExpirationMinutes = 60
            });

        var service =
            new JwtTokenService(options);

        var user = new User(
            "Test Yöneticisi",
            "admin@example.com",
            "example-password-hash",
            UserRole.Admin,
            Guid.NewGuid());

        // Act
        var result =
            service.CreateToken(user);

        // Assert
        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.True(
            result.ExpiresAt > DateTime.UtcNow);

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var token =
            tokenHandler.ReadJwtToken(
                result.AccessToken);

        Assert.Equal(
            "TestIssuer",
            token.Issuer);

        Assert.Contains(
            "TestAudience",
            token.Audiences);

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Value ==
                user.Id.ToString());

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Value ==
                UserRole.Admin.ToString());

        Assert.DoesNotContain(
            token.Claims,
            claim =>
                claim.Value ==
                user.PasswordHash);
        Assert.Contains(
    token.Claims,
    claim =>
        claim.Type == JwtRegisteredClaimNames.Sub &&
        claim.Value == user.Id.ToString());

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == "role" &&
                claim.Value == UserRole.Admin.ToString());

        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == JwtRegisteredClaimNames.Email &&
                claim.Value == user.Email);
    }
}