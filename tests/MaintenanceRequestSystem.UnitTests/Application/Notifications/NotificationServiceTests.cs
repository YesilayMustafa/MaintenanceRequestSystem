using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Notifications.Dtos;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Notifications;

public sealed class NotificationServiceTests
{
    [Fact]
    public async Task AddAsync_ExcludesActorAndDuplicateRecipients()
    {
        var repository = new FakeNotificationRepository();
        var service = new NotificationService(repository);
        var actorId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        await service.AddAsync(
            actorId,
            [actorId, recipientId, recipientId, Guid.Empty],
            NotificationType.TicketAssigned,
            "Atama",
            "Talep atandı.",
            Guid.NewGuid());

        var notification = Assert.Single(repository.Notifications);
        Assert.Equal(recipientId, notification.UserId);
        Assert.Equal(0, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedPagedResult()
    {
        var repository = new FakeNotificationRepository();
        repository.Notifications.Add(new Notification(
            Guid.NewGuid(),
            NotificationType.TicketResolved,
            "Çözüldü",
            "Talep çözüldü."));
        var service = new NotificationService(repository);

        var result = await service.GetPagedAsync(
            repository.Notifications[0].UserId,
            new NotificationListQuery { PageNumber = 1, PageSize = 10 });

        var item = Assert.Single(result.Items);
        Assert.Equal("TicketResolved", item.Type);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetPagedAsync_WithInvalidPageSize_ThrowsValidationException()
    {
        var service = new NotificationService(new FakeNotificationRepository());

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.GetPagedAsync(
                Guid.NewGuid(),
                new NotificationListQuery { PageSize = 101 }));
    }

    [Fact]
    public async Task MarkAsReadAsync_IsIdempotent()
    {
        var repository = new FakeNotificationRepository();
        var notification = new Notification(
            Guid.NewGuid(),
            NotificationType.TicketCommentAdded,
            "Yorum",
            "Yorum eklendi.");
        repository.Notifications.Add(notification);
        var service = new NotificationService(repository);

        await service.MarkAsReadAsync(notification.UserId, notification.Id);
        var firstReadAt = notification.ReadAt;
        await service.MarkAsReadAsync(notification.UserId, notification.Id);

        Assert.True(notification.IsRead);
        Assert.Equal(firstReadAt, notification.ReadAt);
        Assert.Equal(2, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task MarkAsReadAsync_ForAnotherUser_ThrowsKeyNotFoundException()
    {
        var repository = new FakeNotificationRepository();
        var notification = new Notification(
            Guid.NewGuid(),
            NotificationType.TicketAssigned,
            "Atama",
            "Talep atandı.");
        repository.Notifications.Add(notification);
        var service = new NotificationService(repository);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.MarkAsReadAsync(Guid.NewGuid(), notification.Id));
    }

    [Fact]
    public async Task MarkAllAsReadAsync_OnlyMarksCurrentUsersNotifications()
    {
        var repository = new FakeNotificationRepository();
        var userId = Guid.NewGuid();
        var ownNotification = CreateNotification(userId);
        var otherNotification = CreateNotification(Guid.NewGuid());
        repository.Notifications.AddRange([ownNotification, otherNotification]);
        var service = new NotificationService(repository);

        await service.MarkAllAsReadAsync(userId);

        Assert.True(ownNotification.IsRead);
        Assert.False(otherNotification.IsRead);
        Assert.Equal(1, repository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyCurrentUsersUnreadNotifications()
    {
        var repository = new FakeNotificationRepository();
        var userId = Guid.NewGuid();
        repository.Notifications.AddRange([
            CreateNotification(userId),
            CreateNotification(userId),
            CreateNotification(Guid.NewGuid())
        ]);
        repository.Notifications[1].MarkAsRead();
        var service = new NotificationService(repository);

        var result = await service.GetUnreadCountAsync(userId);

        Assert.Equal(1, result.Count);
    }

    private static Notification CreateNotification(Guid userId)
    {
        return new Notification(
            userId,
            NotificationType.TicketStatusChanged,
            "Durum",
            "Talep durumu değişti.");
    }

    private sealed class FakeNotificationRepository
        : INotificationRepository
    {
        public List<Notification> Notifications { get; } = [];
        public int SaveChangesCallCount { get; private set; }

        public Task AddRangeAsync(
            IReadOnlyCollection<Notification> notifications,
            CancellationToken cancellationToken = default)
        {
            Notifications.AddRange(notifications);
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<Notification> Items, int TotalCount)>
            GetPagedAsync(
                Guid userId,
                NotificationListQuery query,
                CancellationToken cancellationToken = default)
        {
            var items = Notifications
                .Where(notification =>
                    notification.UserId == userId &&
                    (!query.UnreadOnly || !notification.IsRead))
                .Skip((query.PageNumber - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToList();
            var count = Notifications.Count(notification =>
                notification.UserId == userId &&
                (!query.UnreadOnly || !notification.IsRead));

            return Task.FromResult<(
                IReadOnlyList<Notification> Items,
                int TotalCount)>((items, count));
        }

        public Task<int> GetUnreadCountAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.Count(notification =>
                notification.UserId == userId &&
                !notification.IsRead));
        }

        public Task<Notification?> GetByIdForUserAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Notifications.FirstOrDefault(notification =>
                notification.Id == id &&
                notification.UserId == userId));
        }

        public Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Notification>>(
                Notifications.Where(notification =>
                    notification.UserId == userId &&
                    !notification.IsRead).ToList());
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }
}
