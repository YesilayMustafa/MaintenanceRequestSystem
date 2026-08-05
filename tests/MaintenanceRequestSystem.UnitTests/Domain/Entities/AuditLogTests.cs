using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.UnitTests.Domain.Entities;

public sealed class AuditLogTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesAuditLog()
    {
        var performedByUserId =
            Guid.NewGuid();

        var entityId =
            Guid.NewGuid();

        var auditLog =
            new AuditLog(
                performedByUserId,
                "  TicketPriorityChanged  ",
                "  Ticket  ",
                $"  {entityId}  ",
                "{\"priority\":\"Medium\"}",
                "{\"priority\":\"Critical\"}");

        Assert.NotEqual(
            Guid.Empty,
            auditLog.Id);

        Assert.Equal(
            performedByUserId,
            auditLog.PerformedByUserId);

        Assert.Equal(
            "TicketPriorityChanged",
            auditLog.Action);

        Assert.Equal(
            "Ticket",
            auditLog.EntityName);

        Assert.Equal(
            entityId.ToString(),
            auditLog.EntityId);

        Assert.Equal(
            "{\"priority\":\"Medium\"}",
            auditLog.OldValues);

        Assert.Equal(
            "{\"priority\":\"Critical\"}",
            auditLog.NewValues);

        Assert.NotEqual(
            default,
            auditLog.CreatedAt);
    }

    [Fact]
    public void Constructor_WithEmptyPerformedByUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new AuditLog(
                Guid.Empty,
                "TicketCancelled",
                "Ticket",
                Guid.NewGuid().ToString()));
    }

    [Fact]
    public void Constructor_WithTooLongAction_ThrowsArgumentException()
    {
        var tooLongAction =
            new string(
                'a',
                AuditLog.MaxActionLength + 1);

        Assert.Throws<ArgumentException>(
            () => new AuditLog(
                Guid.NewGuid(),
                tooLongAction,
                "Ticket",
                Guid.NewGuid().ToString()));
    }

    [Fact]
    public void Constructor_WithInvalidJson_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new AuditLog(
                Guid.NewGuid(),
                "TicketPriorityChanged",
                "Ticket",
                Guid.NewGuid().ToString(),
                "bu geçerli json değil",
                null));
    }
}
