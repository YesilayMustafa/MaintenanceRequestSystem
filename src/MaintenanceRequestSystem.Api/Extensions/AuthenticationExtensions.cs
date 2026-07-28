using System.IdentityModel.Tokens.Jwt;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace MaintenanceRequestSystem.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions =
            configuration
                .GetSection(JwtOptions.SectionName)
                .Get<JwtOptions>()
            ?? throw new InvalidOperationException(
                "JWT ayarları bulunamadı.");

        byte[] signingKeyBytes;

        try
        {
            signingKeyBytes =
                Convert.FromBase64String(
                    jwtOptions.SigningKey);
        }
        catch (FormatException exception)
        {
            throw new InvalidOperationException(
                "JWT imza anahtarı geçerli Base64 formatında değil.",
                exception);
        }

        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT imza anahtarı en az 32 byte olmalıdır.");
        }

        var securityKey =
            new SymmetricSecurityKey(signingKeyBytes);

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                options.TokenValidationParameters =
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = securityKey,

                        ValidateIssuer = true,
                        ValidIssuer = jwtOptions.Issuer,

                        ValidateAudience = true,
                        ValidAudience = jwtOptions.Audience,

                        ValidateLifetime = true,
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,

                        ValidAlgorithms =
                            new[]
                            {
                                SecurityAlgorithms.HmacSha256
                            },

                        ClockSkew = TimeSpan.FromMinutes(1),

                        NameClaimType =
                            JwtRegisteredClaimNames.Name,

                        RoleClaimType = "role"
                    };
            });

        services.AddAuthorization();

        return services;
    }
}