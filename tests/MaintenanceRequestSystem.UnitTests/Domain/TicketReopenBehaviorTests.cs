using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void Reopen_WhenTicketIsClosed_ChangesStatusAndClearsResolutionFields()
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

        ticket.Reopen(
            "  Sorun tekrar oluştu.  ",
            creatorId);

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Null(ticket.ResolutionDescription);
        Assert.Null(ticket.ResolvedAt);
        Assert.Null(ticket.ClosedAt);
        Assert.Null(ticket.WaitingReason);

        Assert.Equal(
            technicianId,
            ticket.AssignedTechnicianId);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.Closed,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.InProgress,
            history.NewStatus);

        Assert.Equal(
            creatorId,
            history.PerformedByUserId);

        Assert.Contains(
            "Sorun tekrar oluştu.",
            history.Description);
    }

    [Fact]
    public void Reopen_WithEmptyReason_ThrowsArgumentException()
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
            "Sorun giderildi.",
            technicianId);

        ticket.Close(
            creatorId);

        Assert.Throws<ArgumentException>(
            () => ticket.Reopen(
                "   ",
                creatorId));

        Assert.Equal(
            TicketStatus.Closed,
            ticket.Status);

        Assert.NotNull(ticket.ClosedAt);
    }

    [Fact]
    public void Reopen_WhenTicketIsResolved_ThrowsArgumentException()
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
            "Sorun giderildi.",
            technicianId);

        Assert.Throws<ArgumentException>(
            () => ticket.Reopen(
                "Sorun yeniden oluştu.",
                creatorId));

        Assert.Equal(
            TicketStatus.Resolved,
            ticket.Status);
    }

    [Fact]
    public void Reopen_WithTooLongReason_ThrowsArgumentException()
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
            "Sorun giderildi.",
            technicianId);

        ticket.Close(
            creatorId);

        var tooLongReason =
            new string(
                'a',
                Ticket.MaxReopenReasonLength + 1);

        Assert.Throws<ArgumentException>(
            () => ticket.Reopen(
                tooLongReason,
                creatorId));

        Assert.Equal(
            TicketStatus.Closed,
            ticket.Status);
    }
}
