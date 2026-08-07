using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    [Fact]
    public void Resolve_WithMaximumLengthDescription_CreatesUntruncatedHistory()
    {
        // Arrange
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

        var resolutionDescription =
            new string(
                'a',
                Ticket.MaxResolutionDescriptionLength);

        // Act
        ticket.Resolve(
            resolutionDescription,
            technicianId);

        // Assert
        var history =
            ticket.Histories.Last();

        var expectedDescription =
            $"Talep çözüldü: {resolutionDescription}";

        Assert.Equal(
            expectedDescription,
            history.Description);

        Assert.Equal(
            2015,
            history.Description.Length);
    }

    [Fact]
    public void Resolve_ByAssignedTechnician_ChangesStatusAndStoresDescription()
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
            "  Ağ yapılandırması düzeltildi.  ",
            technicianId);

        Assert.Equal(
            TicketStatus.Resolved,
            ticket.Status);

        Assert.Equal(
            "Ağ yapılandırması düzeltildi.",
            ticket.ResolutionDescription);

        Assert.NotNull(ticket.ResolvedAt);
        Assert.NotNull(ticket.UpdatedAt);

        var history =
            ticket.Histories.Last();

        Assert.Equal(
            TicketStatus.InProgress,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Resolved,
            history.NewStatus);
    }

    [Fact]
    public void Resolve_WithEmptyDescription_ThrowsArgumentException()
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
            () => ticket.Resolve(
                "   ",
                technicianId));

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);
    }

    [Fact]
    public void Resolve_ByDifferentTechnician_ThrowsArgumentException()
    {
        var ticket =
            new Ticket(
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

        Assert.Throws<ArgumentException>(
            () => ticket.Resolve(
                "Sorun giderildi.",
                Guid.NewGuid()));

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);
    }

    [Fact]
    public void Resolve_WhenTicketIsWaiting_ThrowsArgumentException()
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
            "Parça bekleniyor.",
            technicianId);

        Assert.Throws<ArgumentException>(
            () => ticket.Resolve(
                "Sorun giderildi.",
                technicianId));

        Assert.Equal(
            TicketStatus.Waiting,
            ticket.Status);
    }
}
