using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void Constructor_WithValidValues_NormalizesAndCreatesOpenTicket()
    {
        var assetId = Guid.NewGuid();
        var createdByUserId = Guid.NewGuid();

        var ticket = new Ticket(
            assetId,
            createdByUserId,
            "  Bilgisayar açılmıyor  ",
            "  Güç düğmesine basıldığında cihaz açılmıyor.  ",
            TicketPriority.High);

        Assert.NotEqual(Guid.Empty, ticket.Id);
        Assert.Equal("Bilgisayar açılmıyor", ticket.Title);

        Assert.Equal(
            "Güç düğmesine basıldığında cihaz açılmıyor.",
            ticket.Description);

        Assert.Equal(TicketPriority.High, ticket.Priority);
        Assert.Equal(TicketStatus.Open, ticket.Status);
        Assert.Equal(assetId, ticket.AssetId);
        Assert.Equal(createdByUserId, ticket.CreatedByUserId);
        Assert.Null(ticket.AssignedTechnicianId);
        Assert.Null(ticket.WaitingReason);
        Assert.Null(ticket.ResolutionDescription);
        Assert.Null(ticket.UpdatedAt);
        Assert.Null(ticket.ResolvedAt);
        Assert.Null(ticket.ClosedAt);
    }

    [Fact]
    public void Constructor_WithEmptyAssetId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Ticket(
                Guid.Empty,
                Guid.NewGuid(),
                "Test talebi",
                "Test açıklaması",
                TicketPriority.Medium));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new Ticket(
                Guid.NewGuid(),
                Guid.Empty,
                "Test talebi",
                "Test açıklaması",
                TicketPriority.Medium));
    }

    [Fact]
    public void Constructor_WithInvalidPriority_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Test talebi",
                "Test açıklaması",
                (TicketPriority)999));
    }

    [Fact]
    public void Constructor_WithTooLongTitle_ThrowsArgumentException()
    {
        var title =
            new string(
                'A',
                Ticket.MaxTitleLength + 1);

        Assert.Throws<ArgumentException>(
            () => new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                title,
                "Test açıklaması",
                TicketPriority.Low));
    }

    [Fact]
    public void Constructor_WithTooLongDescription_ThrowsArgumentException()
    {
        var description =
            new string(
                'A',
                Ticket.MaxDescriptionLength + 1);

        Assert.Throws<ArgumentException>(
            () => new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Test talebi",
                description,
                TicketPriority.Low));
    }
}
