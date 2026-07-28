using System.IdentityModel.Tokens.Jwt;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MaintenanceRequestSystem.Api.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services)
    {
        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

        services
            .AddOptions<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>(
                (options, jwtOptionsAccessor) =>
                {
                    var jwtOptions =
                        jwtOptionsAccessor.Value;

                    var signingKeyBytes =
                        Convert.FromBase64String(
                            jwtOptions.SigningKey);

                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    signingKeyBytes),

                            ValidateIssuer = true,
                            ValidIssuer = jwtOptions.Issuer,

                            ValidateAudience = true,
                            ValidAudience =
                                jwtOptions.Audience,

                            ValidateLifetime = true,
                            RequireExpirationTime = true,
                            RequireSignedTokens = true,

                            ValidAlgorithms =
                                new[]
                                {
                                    SecurityAlgorithms.HmacSha256
                                },

                            ClockSkew =
                                TimeSpan.FromMinutes(1),

                            NameClaimType =
                                JwtRegisteredClaimNames.Name,

                            RoleClaimType = "role"
                        };
                });

        services.AddAuthorization();

        return services;
    }
}