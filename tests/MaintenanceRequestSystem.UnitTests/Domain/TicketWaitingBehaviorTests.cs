using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void PutOnHold_ByAssignedTechnician_ChangesStatusAndStoresReason()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
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
            "  Yedek parça bekleniyor.  ",
            technicianId);

        Assert.Equal(
            TicketStatus.Waiting,
            ticket.Status);

        Assert.Equal(
            "Yedek parça bekleniyor.",
            ticket.WaitingReason);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.InProgress,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Waiting,
            history.NewStatus);
    }

    [Fact]
    public void PutOnHold_WithEmptyReason_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
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
            () => ticket.PutOnHold(
                "   ",
                technicianId));

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);
    }

    [Fact]
    public void Resume_ByAssignedTechnician_ReturnsTicketToInProgress()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
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

        ticket.Resume(
            technicianId);

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Null(
            ticket.WaitingReason);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.Waiting,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.InProgress,
            history.NewStatus);
    }

    [Fact]
    public void Resume_ByDifferentTechnician_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        var assignedTechnicianId =
            Guid.NewGuid();

        ticket.Assign(
            assignedTechnicianId,
            Guid.NewGuid());

        ticket.StartProgress(
            assignedTechnicianId);

        ticket.PutOnHold(
            "Yedek parça bekleniyor.",
            assignedTechnicianId);

        Assert.Throws<ArgumentException>(
            () => ticket.Resume(
                Guid.NewGuid()));

        Assert.Equal(
            TicketStatus.Waiting,
            ticket.Status);
    }
}
