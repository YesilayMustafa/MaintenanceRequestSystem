using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void SoftDelete_WhenTicketIsClosed_MarksTicketAsDeleted()
    {
        var creatorId =
            Guid.NewGuid();

        var technicianId =
            Guid.NewGuid();

        var adminId =
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
            adminId);

        ticket.StartProgress(
            technicianId);

        ticket.Resolve(
            "Sorun giderildi.",
            technicianId);

        ticket.Close(
            creatorId);

        ticket.SoftDelete(
            adminId);

        Assert.True(
            ticket.IsDeleted);

        Assert.NotNull(
            ticket.DeletedAt);

        Assert.Equal(
            adminId,
            ticket.DeletedByUserId);

        Assert.NotNull(
            ticket.UpdatedAt);
    }

    [Fact]
    public void SoftDelete_WhenTicketIsCancelled_MarksTicketAsDeleted()
    {
        var creatorId =
            Guid.NewGuid();

        var adminId =
            Guid.NewGuid();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creatorId,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        ticket.Cancel(
            creatorId);

        ticket.SoftDelete(
            adminId);

        Assert.True(
            ticket.IsDeleted);

        Assert.Equal(
            adminId,
            ticket.DeletedByUserId);
    }

    [Fact]
    public void SoftDelete_WhenTicketIsOpen_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        Assert.Throws<ArgumentException>(
            () => ticket.SoftDelete(
                Guid.NewGuid()));

        Assert.False(
            ticket.IsDeleted);

        Assert.Null(
            ticket.DeletedAt);

        Assert.Null(
            ticket.DeletedByUserId);
    }

    [Fact]
    public void SoftDelete_WhenTicketIsAlreadyDeleted_ThrowsArgumentException()
    {
        var creatorId =
            Guid.NewGuid();

        var adminId =
            Guid.NewGuid();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creatorId,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        ticket.Cancel(
            creatorId);

        ticket.SoftDelete(
            adminId);

        Assert.Throws<ArgumentException>(
            () => ticket.SoftDelete(
                adminId));

        Assert.True(
            ticket.IsDeleted);
    }
}
