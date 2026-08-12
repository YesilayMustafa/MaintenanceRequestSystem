using MaintenanceRequestSystem.Application.Notifications.Dtos;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly ApplicationDbContext _context;

    public NotificationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(
        IReadOnlyCollection<Notification> notifications,
        CancellationToken cancellationToken = default)
    {
        await _context.Notifications.AddRangeAsync(
            notifications,
            cancellationToken);
    }

    public async Task<(IReadOnlyList<Notification> Items, int TotalCount)>
        GetPagedAsync(
            Guid userId,
            NotificationListQuery query,
            CancellationToken cancellationToken = default)
    {
        var notificationQuery = _context.Notifications
            .AsNoTracking()
            .Include(notification => notification.Ticket)
            .Where(notification => notification.UserId == userId);

        if (query.UnreadOnly)
        {
            notificationQuery = notificationQuery.Where(
                notification => !notification.IsRead);
        }

        var totalCount = await notificationQuery.CountAsync(cancellationToken);
        var offset = ((long)query.PageNumber - 1L) * query.PageSize;
        var items = await notificationQuery
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenBy(notification => notification.Id)
            .Skip(checked((int)offset))
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public Task<int> GetUnreadCountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Notifications.CountAsync(
            notification =>
                notification.UserId == userId &&
                !notification.IsRead,
            cancellationToken);
    }

    public Task<Notification?> GetByIdForUserAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _context.Notifications.FirstOrDefaultAsync(
            notification =>
                notification.Id == id &&
                notification.UserId == userId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Notification>> GetUnreadForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Notifications
            .Where(notification =>
                notification.UserId == userId &&
                !notification.IsRead)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
