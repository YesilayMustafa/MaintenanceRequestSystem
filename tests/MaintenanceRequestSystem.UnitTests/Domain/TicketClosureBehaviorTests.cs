using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void Close_WhenTicketIsResolved_ChangesStatusAndAddsHistory()
    {
        var creatorId =
            Guid.NewGuid();

        var technicianId =
            Guid.NewGuid();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creatorId,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        ticket.Assign(
            technicianId,
            Guid.NewGuid());

        ticket.StartProgress(
            technicianId);

        ticket.Resolve(
            "Ağ yapılandırması düzeltildi.",
            technicianId);

        ticket.Close(
            creatorId);

        Assert.Equal(
            TicketStatus.Closed,
            ticket.Status);

        Assert.NotNull(ticket.ClosedAt);
        Assert.NotNull(ticket.UpdatedAt);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.Resolved,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Closed,
            history.NewStatus);

        Assert.Equal(
            creatorId,
            history.PerformedByUserId);
    }

    [Fact]
    public void Close_WhenTicketIsInProgress_ThrowsArgumentException()
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
            () => ticket.Close(
                Guid.NewGuid()));

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Null(
            ticket.ClosedAt);
    }

    [Fact]
    public void Close_WithEmptyPerformedByUserId_ThrowsArgumentException()
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

        ticket.Resolve(
            "Sorun giderildi.",
            technicianId);

        Assert.Throws<ArgumentException>(
            () => ticket.Close(
                Guid.Empty));

        Assert.Equal(
            TicketStatus.Resolved,
            ticket.Status);
    }
}
