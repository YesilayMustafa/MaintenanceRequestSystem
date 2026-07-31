using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed class TicketCommentBehaviorTests
{
    [Fact]
    public void Constructor_WithValidValues_NormalizesAndCreatesComment()
    {
        var ticketId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var comment = new TicketComment(
            ticketId,
            userId,
            "  Güç adaptörü kontrol edildi.  ");

        Assert.NotEqual(Guid.Empty, comment.Id);
        Assert.Equal(ticketId, comment.TicketId);
        Assert.Equal(userId, comment.UserId);

        Assert.Equal(
            "Güç adaptörü kontrol edildi.",
            comment.Content);

        Assert.InRange(
            comment.CreatedAt,
            DateTime.UtcNow.AddSeconds(-2),
            DateTime.UtcNow);
    }

    [Fact]
    public void Constructor_WithEmptyTicketId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new TicketComment(
                Guid.Empty,
                Guid.NewGuid(),
                "Test yorumu"));
    }

    [Fact]
    public void Constructor_WithEmptyUserId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(
            () => new TicketComment(
                Guid.NewGuid(),
                Guid.Empty,
                "Test yorumu"));
    }

    [Fact]
    public void Constructor_WithTooLongContent_ThrowsArgumentException()
    {
        var content =
            new string(
                'A',
                TicketComment.MaxContentLength + 1);

        Assert.Throws<ArgumentException>(
            () => new TicketComment(
                Guid.NewGuid(),
                Guid.NewGuid(),
                content));
    }
}