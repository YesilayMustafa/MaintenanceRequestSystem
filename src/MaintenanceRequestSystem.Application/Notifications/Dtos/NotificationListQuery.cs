namespace MaintenanceRequestSystem.Application.Notifications.Dtos;

public sealed class NotificationListQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public bool UnreadOnly { get; init; }
}
