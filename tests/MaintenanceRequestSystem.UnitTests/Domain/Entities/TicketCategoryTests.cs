using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain.Entities;

public sealed class TicketCategoryTests
{
    [Fact]
    public void Constructor_NormalizesNameAndDescription()
    {
        var category = new TicketCategory(
            "  yazılım  ",
            "  Uygulama sorunları  ");

        Assert.Equal("yazılım", category.Name);
        Assert.Equal("YAZILIM", category.NormalizedName);
        Assert.Equal("Uygulama sorunları", category.Description);
        Assert.True(category.IsActive);
    }

    [Fact]
    public void NormalizeName_TreatsTurkishCaseAndWhitespaceAsEqual()
    {
        Assert.Equal(
            TicketCategory.NormalizeName("Yazılım"),
            TicketCategory.NormalizeName("  YAZILIM  "));
    }

    [Fact]
    public void UpdateDetails_ChangesNormalizedValuesAndTimestamp()
    {
        var category = new TicketCategory("Donanım");

        category.UpdateDetails("  Ağ  ", "  Bağlantı  ");

        Assert.Equal("Ağ", category.Name);
        Assert.Equal("AĞ", category.NormalizedName);
        Assert.Equal("Bağlantı", category.Description);
        Assert.NotNull(category.UpdatedAt);
    }

    [Fact]
    public void ActivateAndDeactivate_PreserveLifecycle()
    {
        var category = new TicketCategory("Donanım");

        category.Deactivate();
        Assert.False(category.IsActive);

        category.Activate();
        Assert.True(category.IsActive);
    }

    [Fact]
    public void Constructor_WithBlankName_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TicketCategory("   "));
    }

    [Fact]
    public void TicketChangeCategory_ChangesIdAndCreatesHistory()
    {
        var ticket = new Ticket(
            "REQ-2099-000001",
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test talebi",
            "Test açıklaması",
            TicketPriority.Medium);
        var newCategoryId = Guid.NewGuid();
        var adminId = Guid.NewGuid();

        ticket.ChangeCategory(
            newCategoryId,
            adminId,
            "Donanım",
            "Yazılım");

        Assert.Equal(newCategoryId, ticket.CategoryId);
        Assert.NotNull(ticket.UpdatedAt);
        var history = Assert.Single(ticket.Histories);
        Assert.Equal(adminId, history.PerformedByUserId);
        Assert.Equal(ticket.Status, history.OldStatus);
        Assert.Equal(ticket.Status, history.NewStatus);
        Assert.Contains("Donanım", history.Description);
        Assert.Contains("Yazılım", history.Description);
    }
}
