using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void ChangePriority_WhenTicketIsActive_ChangesPriority()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        var performedByUserId =
            Guid.NewGuid();

        ticket.ChangePriority(
            TicketPriority.Critical,
            performedByUserId);

        Assert.Equal(
            TicketPriority.Critical,
            ticket.Priority);

        Assert.NotNull(
            ticket.UpdatedAt);
    }

    [Fact]
    public void ChangePriority_WithSamePriority_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        Assert.Throws<ArgumentException>(
            () => ticket.ChangePriority(
                TicketPriority.High,
                Guid.NewGuid()));

        Assert.Equal(
            TicketPriority.High,
            ticket.Priority);
    }

    [Fact]
    public void ChangePriority_WithInvalidPriority_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        var invalidPriority =
            (TicketPriority)999;

        Assert.Throws<ArgumentException>(
            () => ticket.ChangePriority(
                invalidPriority,
                Guid.NewGuid()));

        Assert.Equal(
            TicketPriority.Medium,
            ticket.Priority);
    }

    [Fact]
    public void ChangePriority_WhenTicketIsCancelled_ThrowsArgumentException()
    {
        var creatorId =
            Guid.NewGuid();

        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                creatorId,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        ticket.Cancel(
            creatorId);

        Assert.Throws<ArgumentException>(
            () => ticket.ChangePriority(
                TicketPriority.Critical,
                Guid.NewGuid()));

        Assert.Equal(
            TicketPriority.Medium,
            ticket.Priority);
    }
}
