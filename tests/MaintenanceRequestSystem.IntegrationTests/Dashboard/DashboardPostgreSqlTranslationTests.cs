using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MaintenanceRequestSystem.IntegrationTests.Dashboard;

public sealed class DashboardPostgreSqlTranslationTests
{
    [Fact]
    public async Task GetAsync_WithNpgsqlProvider_TranslatesSlaCountQuery()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(
                "Host=127.0.0.1;Port=1;Database=translation_only;" +
                "Username=translation_only;Password=translation_only;Timeout=1")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var repository = new DashboardRepository(context);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repository.GetAsync(Guid.NewGuid(), UserRole.Admin));

        Assert.IsType<NpgsqlException>(exception.InnerException);
        Assert.DoesNotContain(
            "Unable to cast object of type 'System.Double' to type 'System.TimeSpan'",
            exception.ToString(),
            StringComparison.Ordinal);
    }
}
