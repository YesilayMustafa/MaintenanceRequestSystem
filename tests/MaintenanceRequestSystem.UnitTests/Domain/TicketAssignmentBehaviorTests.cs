using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void Reassign_WhenTicketIsAssigned_ChangesTechnicianAndAddsHistory()
    {
        var ticket =
            new Ticket(
                "REQ-2000-999999",
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var firstTechnicianId =
            Guid.NewGuid();

        var secondTechnicianId =
            Guid.NewGuid();

        var adminId =
            Guid.NewGuid();

        ticket.Assign(
            firstTechnicianId,
            adminId);

        var previousUpdatedAt =
            ticket.UpdatedAt;

        ticket.Reassign(
            secondTechnicianId,
            adminId);

        Assert.Equal(
            secondTechnicianId,
            ticket.AssignedTechnicianId);

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.True(
            ticket.UpdatedAt >= previousUpdatedAt);

        Assert.Equal(
            2,
            ticket.Histories.Count);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.Assigned,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Assigned,
            history.NewStatus);

        Assert.Equal(
            adminId,
            history.PerformedByUserId);
    }

    [Fact]
    public void Reassign_WithSameTechnician_ThrowsArgumentException()
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

        var adminId =
            Guid.NewGuid();

        ticket.Assign(
            technicianId,
            adminId);

        Assert.Throws<ArgumentException>(
            () => ticket.Reassign(
                technicianId,
                adminId));

        Assert.Single(
            ticket.Histories);
    }

    [Fact]
    public void Reassign_WhenTicketIsOpen_ThrowsArgumentException()
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
            () => ticket.Reassign(
                Guid.NewGuid(),
                Guid.NewGuid()));

        Assert.Null(
            ticket.AssignedTechnicianId);

        Assert.Empty(
            ticket.Histories);
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
}
