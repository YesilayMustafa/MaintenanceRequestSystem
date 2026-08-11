using System.IdentityModel.Tokens.Jwt;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using MaintenanceRequestSystem.Application.Authentication;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
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

                            RoleClaimType =
                                AuthenticationClaimNames.Role
                        };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var userIdValue =
                                context.Principal?.FindFirst(
                                    JwtRegisteredClaimNames.Sub)?.Value;

                            var roleValue =
                                context.Principal?.FindFirst(
                                    AuthenticationClaimNames.Role)?.Value;

                            var securityVersionValue =
                                context.Principal?.FindFirst(
                                    AuthenticationClaimNames.SecurityVersion)?.Value;

                            if (!Guid.TryParse(userIdValue, out var userId) ||
                                !Enum.TryParse<UserRole>(roleValue, out var role) ||
                                !Enum.IsDefined(role) ||
                                !int.TryParse(
                                    securityVersionValue,
                                    out var securityVersion))
                            {
                                context.Fail(
                                    "Kimlik doğrulama claim'leri geçersiz.");
                                return;
                            }

                            var userRepository =
                                context.HttpContext.RequestServices
                                    .GetRequiredService<IUserRepository>();

                            var user = await userRepository.GetByIdAsync(
                                userId,
                                context.HttpContext.RequestAborted);

                            if (user is null ||
                                !user.IsOperational ||
                                user.SecurityVersion != securityVersion ||
                                user.Role != role)
                            {
                                context.Fail(
                                    "Kimlik doğrulama bilgileri güncel değil.");
                            }
                        }
                    };
                });

        services.AddAuthorization();

        return services;
    }
}
