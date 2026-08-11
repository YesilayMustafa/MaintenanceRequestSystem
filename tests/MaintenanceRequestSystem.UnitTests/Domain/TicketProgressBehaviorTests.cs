using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void StartProgress_ByAssignedTechnician_ChangesStatusAndAddsHistory()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var technicianId =
            Guid.NewGuid();

        ticket.Assign(
            technicianId,
            Guid.NewGuid());

        ticket.StartProgress(
            technicianId);

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Equal(
            technicianId,
            ticket.AssignedTechnicianId);

        Assert.NotNull(
            ticket.UpdatedAt);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.Assigned,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.InProgress,
            history.NewStatus);

        Assert.Equal(
            technicianId,
            history.PerformedByUserId);
    }

    [Fact]
    public void StartProgress_ByDifferentTechnician_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        ticket.Assign(
            Guid.NewGuid(),
            Guid.NewGuid());

        Assert.Throws<ArgumentException>(
            () => ticket.StartProgress(
                Guid.NewGuid()));

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.Single(
            ticket.Histories);
    }

    [Fact]
    public void StartProgress_WhenTicketIsOpen_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        Assert.Throws<ArgumentException>(
            () => ticket.StartProgress(
                Guid.NewGuid()));

        Assert.Equal(
            TicketStatus.Open,
            ticket.Status);

        Assert.Empty(
            ticket.Histories);
    }
}
