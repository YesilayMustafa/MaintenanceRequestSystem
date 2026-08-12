using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Notifications.Dtos;

namespace MaintenanceRequestSystem.Application.Notifications.Interfaces;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(
        Guid currentUserId,
        NotificationListQuery query,
        CancellationToken cancellationToken = default);

    Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);

    Task MarkAsReadAsync(
        Guid currentUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default);

    Task MarkAllAsReadAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default);
}
