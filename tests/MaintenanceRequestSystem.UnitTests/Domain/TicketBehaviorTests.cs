using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed class TicketBehaviorTests
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

    [Fact]
    public void Assign_WithValidIds_AssignsTicketAndCreatesHistory()
    {
        var ticket = CreateTicket();
        var technicianId = Guid.NewGuid();
        var performedByUserId = Guid.NewGuid();
        var beforeAssignment = DateTime.UtcNow;

        ticket.Assign(
            technicianId,
            performedByUserId);

        var afterAssignment = DateTime.UtcNow;

        Assert.Equal(
            technicianId,
            ticket.AssignedTechnicianId);

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.NotNull(ticket.UpdatedAt);

        Assert.InRange(
            ticket.UpdatedAt.Value,
            beforeAssignment,
            afterAssignment);

        Assert.Equal(
            DateTimeKind.Utc,
            ticket.UpdatedAt.Value.Kind);

        var history = Assert.Single(ticket.Histories);

        Assert.Equal(ticket.Id, history.TicketId);
        Assert.Equal(performedByUserId, history.PerformedByUserId);
        Assert.Equal(TicketStatus.Open, history.OldStatus);
        Assert.Equal(TicketStatus.Assigned, history.NewStatus);
        Assert.Equal("Talep teknik personele atandı.", history.Description);
    }

    [Fact]
    public void Assign_WithEmptyTechnicianId_ThrowsArgumentException()
    {
        var ticket = CreateTicket();

        Assert.Throws<ArgumentException>(
            () => ticket.Assign(
                Guid.Empty,
                Guid.NewGuid()));
    }

    [Fact]
    public void Assign_WithEmptyPerformedByUserId_ThrowsArgumentException()
    {
        var ticket = CreateTicket();

        Assert.Throws<ArgumentException>(
            () => ticket.Assign(
                Guid.NewGuid(),
                Guid.Empty));
    }

    [Fact]
    public void Assign_WhenTicketIsAlreadyAssigned_ThrowsArgumentException()
    {
        var ticket = CreateTicket();
        var firstTechnicianId = Guid.NewGuid();

        ticket.Assign(
            firstTechnicianId,
            Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => ticket.Assign(
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Equal(
            firstTechnicianId,
            ticket.AssignedTechnicianId);

        Assert.Equal(TicketStatus.Assigned, ticket.Status);
        Assert.Single(ticket.Histories);
    }

    private static Ticket CreateTicket()
    {
        return new Ticket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test talebi",
            "Test açıklaması",
            TicketPriority.Medium);
    }
}
