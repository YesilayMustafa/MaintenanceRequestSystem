using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Notifications.Services;

public sealed class NullNotificationWriter : INotificationWriter
{
    public Task AddAsync(
        Guid actorUserId,
        IEnumerable<Guid> recipientUserIds,
        NotificationType type,
        string title,
        string message,
        Guid? ticketId = null,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
