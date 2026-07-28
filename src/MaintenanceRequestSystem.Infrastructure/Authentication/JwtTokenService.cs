using System;
using System.Collections.Generic;
using System.Text;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MaintenanceRequestSystem.Infrastructure.Authentication;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _options;

    public JwtTokenService(
        IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public AccessTokenResult CreateToken(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var now = DateTime.UtcNow;

        var expiresAt =
            now.AddMinutes(_options.ExpirationMinutes);

        var claims = new List<Claim>
{
    new(
        JwtRegisteredClaimNames.Sub,
        user.Id.ToString()),

    new(
        JwtRegisteredClaimNames.Jti,
        Guid.NewGuid().ToString()),

    new(
        JwtRegisteredClaimNames.Name,
        user.FullName),

    new(
        JwtRegisteredClaimNames.Email,
        user.Email),

    new(
        "role",
        user.Role.ToString())
};

        var keyBytes =
            Convert.FromBase64String(
                _options.SigningKey);

        var securityKey =
            new SymmetricSecurityKey(keyBytes);

        var signingCredentials =
            new SigningCredentials(
                securityKey,
                SecurityAlgorithms.HmacSha256);

        var tokenDescriptor =
            new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Issuer = _options.Issuer,
                Audience = _options.Audience,
                NotBefore = now,
                Expires = expiresAt,
                SigningCredentials = signingCredentials
            };

        var tokenHandler =
            new JwtSecurityTokenHandler();

        var securityToken =
            tokenHandler.CreateToken(tokenDescriptor);

        var accessToken =
            tokenHandler.WriteToken(securityToken);

        return new AccessTokenResult(
            accessToken,
            expiresAt);
    }
}