using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore.Storage;
using System.IdentityModel.Tokens.Jwt;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MaintenanceRequestSystem.IntegrationTests.Infrastructure;

public sealed class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    public const string AdminEmail =
        "admin.integration@example.com";

    public const string AdminPassword =
        "AdminTest123!";

    public const string EmployeeEmail =
        "employee.integration@example.com";

    public const string EmployeePassword =
        "EmployeeTest123!";

    private readonly string _databaseName =
    $"MaintenanceRequestSystemTests-{Guid.NewGuid()}";

    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    private const string TestIssuer =
    "MaintenanceRequestSystem.IntegrationTests";

    private const string TestAudience =
        "MaintenanceRequestSystem.TestClient";

    private static readonly byte[] TestSigningKeyBytes =
        Enumerable.Range(1, 32)
            .Select(number => (byte)number)
            .ToArray();

    private static readonly string TestSigningKey =
        Convert.ToBase64String(TestSigningKeyBytes);

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration(
            (_, configurationBuilder) =>
            {
                var signingKeyBytes =
                    Enumerable.Range(1, 32)
                        .Select(number => (byte)number)
                        .ToArray();

                var settings =
                    new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] =
                            "Host=localhost;Database=integration_tests",
                        ["Jwt:Issuer"] = TestIssuer,
                        ["Jwt:Audience"] = TestAudience,
                        ["Jwt:SigningKey"] = TestSigningKey,
                        ["Jwt:ExpirationMinutes"] = "60",

                        ["SeedAdmin:Email"] = AdminEmail,
                        ["SeedAdmin:Password"] = AdminPassword,

                        ["SeedEmployee:Email"] = EmployeeEmail,
                        ["SeedEmployee:Password"] = EmployeePassword
                    };

                configurationBuilder.AddInMemoryCollection(
                    settings);
            });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                DbContextOptions<ApplicationDbContext>>();

            services.RemoveAll<
                IDbContextOptionsConfiguration<
                    ApplicationDbContext>>();

            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(
                options =>
                    options.UseInMemoryDatabase(
                        _databaseName,
                        _databaseRoot));

            // Token üreten JwtTokenService bu ayarları kullanacak.
            services.RemoveAll<IOptions<JwtOptions>>();

            services.AddSingleton<IOptions<JwtOptions>>(
                Options.Create(
                    new JwtOptions
                    {
                        Issuer = TestIssuer,
                        Audience = TestAudience,
                        SigningKey = TestSigningKey,
                        ExpirationMinutes = 60
                    }));

            // Gelen token'ı doğrulayan JwtBearer aynı ayarları kullanacak.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.MapInboundClaims = false;

                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuerSigningKey = true,

                            IssuerSigningKey =
                                new SymmetricSecurityKey(
                                    TestSigningKeyBytes),

                            ValidateIssuer = true,
                            ValidIssuer = TestIssuer,

                            ValidateAudience = true,
                            ValidAudience = TestAudience,

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
        });
    }
}