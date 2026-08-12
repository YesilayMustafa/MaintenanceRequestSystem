using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Notifications.Dtos;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Notifications.Services;

public sealed class NotificationService
    : INotificationService, INotificationWriter
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(
        INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task AddAsync(
        Guid actorUserId,
        IEnumerable<Guid> recipientUserIds,
        NotificationType type,
        string title,
        string message,
        Guid? ticketId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(recipientUserIds);

        var notifications = recipientUserIds
            .Where(userId =>
                userId != Guid.Empty &&
                userId != actorUserId)
            .Distinct()
            .Select(userId => new Notification(
                userId,
                type,
                title,
                message,
                ticketId))
            .ToList();

        if (notifications.Count == 0)
        {
            return;
        }

        await _notificationRepository.AddRangeAsync(
            notifications,
            cancellationToken);
    }

    public async Task<PagedResult<NotificationDto>> GetPagedAsync(
        Guid currentUserId,
        NotificationListQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");
        ArgumentNullException.ThrowIfNull(query);
        ValidateQuery(query);

        var result = await _notificationRepository.GetPagedAsync(
            currentUserId,
            query,
            cancellationToken);
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);

        return new PagedResult<NotificationDto>(
            result.Items.Select(MapToDto).ToList(),
            query.PageNumber,
            query.PageSize,
            result.TotalCount,
            totalPages);
    }

    public async Task<UnreadNotificationCountDto> GetUnreadCountAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");

        return new UnreadNotificationCountDto(
            await _notificationRepository.GetUnreadCountAsync(
                currentUserId,
                cancellationToken));
    }

    public async Task MarkAsReadAsync(
        Guid currentUserId,
        Guid notificationId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");
        EnsureValidId(notificationId, "Geçerli bir bildirim kimliği gereklidir.");

        var notification = await _notificationRepository.GetByIdForUserAsync(
            notificationId,
            currentUserId,
            cancellationToken);

        if (notification is null)
        {
            throw new KeyNotFoundException("Bildirim bulunamadı.");
        }

        notification.MarkAsRead();
        await _notificationRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAllAsReadAsync(
        Guid currentUserId,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");

        var notifications = await _notificationRepository.GetUnreadForUserAsync(
            currentUserId,
            cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        if (notifications.Count > 0)
        {
            await _notificationRepository.SaveChangesAsync(cancellationToken);
        }
    }

    private static NotificationDto MapToDto(Notification notification)
    {
        return new NotificationDto(
            notification.Id,
            notification.Type.ToString(),
            notification.Title,
            notification.Message,
            notification.TicketId,
            notification.Ticket?.TicketNumber,
            notification.IsRead,
            notification.ReadAt,
            notification.CreatedAt);
    }

    private static void ValidateQuery(NotificationListQuery query)
    {
        if (query.PageNumber < 1)
        {
            throw new RequestValidationException("Sayfa numarası en az 1 olmalıdır.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }

        var offset = ((long)query.PageNumber - 1L) * query.PageSize;

        if (offset > int.MaxValue)
        {
            throw new RequestValidationException(
                "İstenen sayfa numarası desteklenen sınırı aşıyor.");
        }
    }

    private static void EnsureValidId(Guid id, string message)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(message);
        }
    }
}
