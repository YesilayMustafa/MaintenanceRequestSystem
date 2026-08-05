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
    public void Reassign_WhenTicketIsAssigned_ChangesTechnicianAndAddsHistory()
    {
        var ticket =
            new Ticket(
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
    public void StartProgress_ByAssignedTechnician_ChangesStatusAndAddsHistory()
    {
        var ticket =
            new Ticket(
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
    public void PutOnHold_ByAssignedTechnician_ChangesStatusAndStoresReason()
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
    [Fact]
    public void ChangePriority_WhenTicketIsActive_ChangesPriority()
    {
        var ticket =
            new Ticket(
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
