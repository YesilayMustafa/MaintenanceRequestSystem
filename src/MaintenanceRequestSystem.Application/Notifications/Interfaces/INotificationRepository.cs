using MaintenanceRequestSystem.Application.Notifications.Dtos;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Notifications.Interfaces;

public interface INotificationRepository
{
    Task AddRangeAsync(
        IReadOnlyCollection<Notification> notifications,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Notification> Items, int TotalCount)> GetPagedAsync(
        Guid userId,
        NotificationListQuery query,
        CancellationToken cancellationToken = default);

    Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Notification?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
