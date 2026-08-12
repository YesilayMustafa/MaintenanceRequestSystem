using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Notifications.Interfaces;

public interface INotificationWriter
{
    Task AddAsync(
        Guid actorUserId,
        IEnumerable<Guid> recipientUserIds,
        NotificationType type,
        string title,
        string message,
        Guid? ticketId = null,
        CancellationToken cancellationToken = default);
}
