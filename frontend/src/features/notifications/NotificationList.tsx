import type { NotificationDto } from "../../types/notifications";

interface NotificationListProps {
    notifications: NotificationDto[];
    activeNotificationId: string | null;
    emptyMessage: string;
    onOpen: (notification: NotificationDto) => void;
}

export function NotificationList({
    notifications,
    activeNotificationId,
    emptyMessage,
    onOpen,
}: NotificationListProps) {
    if (notifications.length === 0) {
        return <p className="empty-state">{emptyMessage}</p>;
    }

    return (
        <ul className="notification-list">
            {notifications.map((notification) => {
                const hasTicketLink = Boolean(
                    notification.ticketId && notification.ticketNumber
                );

                return (
                    <li
                        key={notification.id}
                        className={
                            `notification-item` +
                            (notification.isRead
                                ? ""
                                : " notification-item-unread")
                        }
                    >
                        <button
                            type="button"
                            className="notification-item-button"
                            disabled={activeNotificationId === notification.id}
                            onClick={() => onOpen(notification)}
                        >
                            <span className="notification-title-row">
                                <strong>{notification.title}</strong>
                                {!notification.isRead && (
                                    <span className="notification-new-label">
                                        Yeni
                                    </span>
                                )}
                            </span>
                            <span className="notification-message">
                                {notification.message}
                            </span>
                            <span className="notification-meta">
                                {hasTicketLink && notification.ticketNumber && (
                                    <span>{notification.ticketNumber} · </span>
                                )}
                                {new Date(notification.createdAt)
                                    .toLocaleString("tr-TR")}
                            </span>
                            {activeNotificationId === notification.id && (
                                <span className="notification-progress">
                                    İşleniyor...
                                </span>
                            )}
                        </button>
                    </li>
                );
            })}
        </ul>
    );
}
