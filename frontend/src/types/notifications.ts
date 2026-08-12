export interface NotificationDto {
    id: string;
    type: string;
    title: string;
    message: string;
    ticketId: string | null;
    ticketNumber: string | null;
    isRead: boolean;
    readAt: string | null;
    createdAt: string;
}

export interface NotificationListQuery {
    pageNumber?: number;
    pageSize?: number;
    unreadOnly?: boolean;
}

export interface UnreadNotificationCountDto {
    count: number;
}
