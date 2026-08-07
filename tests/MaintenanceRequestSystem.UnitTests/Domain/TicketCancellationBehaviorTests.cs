using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void Cancel_WhenTicketIsOpen_ChangesStatusAndAddsHistory()
    {
        var creatorId =
            Guid.NewGuid();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creatorId,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        ticket.Cancel(
            creatorId);

        Assert.Equal(
            TicketStatus.Cancelled,
            ticket.Status);

        Assert.NotNull(
            ticket.UpdatedAt);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.Open,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Cancelled,
            history.NewStatus);

        Assert.Equal(
            creatorId,
            history.PerformedByUserId);
    }

    [Fact]
    public void Cancel_WhenTicketIsAssigned_ChangesStatusToCancelled()
    {
        var ticket =
            new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        ticket.Assign(
            Guid.NewGuid(),
            Guid.NewGuid());

        ticket.Cancel(
            Guid.NewGuid());

        Assert.Equal(
            TicketStatus.Cancelled,
            ticket.Status);

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Histories.Last().OldStatus);
    }

    [Fact]
    public void Cancel_WhenTicketIsWaiting_ChangesStatusToCancelled()
    {
        var ticket =
            new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        var technicianId =
            Guid.NewGuid();

        ticket.Assign(
            technicianId,
            Guid.NewGuid());

        ticket.StartProgress(
            technicianId);

        ticket.PutOnHold(
            "Yedek parça bekleniyor.",
            technicianId);

        ticket.Cancel(
            Guid.NewGuid());

        Assert.Equal(
            TicketStatus.Cancelled,
            ticket.Status);

        Assert.Equal(
            TicketStatus.Waiting,
            ticket.Histories.Last().OldStatus);
    }

    [Fact]
    public void Cancel_WhenTicketIsInProgress_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        var technicianId =
            Guid.NewGuid();

        ticket.Assign(
            technicianId,
            Guid.NewGuid());

        ticket.StartProgress(
            technicianId);

        Assert.Throws<ArgumentException>(
            () => ticket.Cancel(
                Guid.NewGuid()));

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);
    }
}
