using System.Security.Cryptography;
using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using Microsoft.IdentityModel.Tokens;

namespace MaintenanceRequestSystem.Infrastructure.Authentication;

public sealed class AccountTokenGenerator
    : IAccountTokenGenerator
{
    private const int TokenSizeInBytes = 32;

    public GeneratedAccountToken Generate()
    {
        var tokenBytes =
            RandomNumberGenerator.GetBytes(TokenSizeInBytes);

        var rawToken =
            Base64UrlEncoder.Encode(tokenBytes);

        return new GeneratedAccountToken(
            rawToken,
            HashToken(rawToken));
    }

    public string HashToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException(
                "Raw token boş olamaz.",
                nameof(rawToken));
        }

        var hashBytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(rawToken));

        return Convert.ToBase64String(hashBytes);
    }
}
