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
using Microsoft.Extensions.Logging;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;

namespace MaintenanceRequestSystem.IntegrationTests.Infrastructure;

public class CustomWebApplicationFactory
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

    private readonly string _attachmentRoot = Path.Combine(
        Path.GetTempPath(),
        "MaintenanceRequestSystemTests",
        Guid.NewGuid().ToString("N"));

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
        builder.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Warning);
        });

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
                        ["Frontend:BaseUrl"] =
                            "https://frontend.integration.example",
                        ["Email:Mode"] = "DevelopmentFile",
                        ["Attachments:StorageRootPath"] = _attachmentRoot,

                        ["SeedAdmin:Email"] = AdminEmail,
                        ["SeedAdmin:Password"] = AdminPassword,
                        ["SeedData:Enabled"] = "true",
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

            services.RemoveAll<IEmailSender>();
            services.AddSingleton<TestEmailSender>();
            services.AddSingleton<IEmailSender>(provider =>
                provider.GetRequiredService<TestEmailSender>());
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(_attachmentRoot))
        {
            Directory.Delete(_attachmentRoot, recursive: true);
        }
    }
}
