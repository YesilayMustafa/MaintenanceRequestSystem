import { apiRequest } from "./httpClient";

import type { PagedResult } from "../types/pagination";
import type {
    NotificationDto,
    NotificationListQuery,
    UnreadNotificationCountDto,
} from "../types/notifications";

export function getNotifications(
    token: string,
    query: NotificationListQuery = {}
): Promise<PagedResult<NotificationDto>> {
    const searchParams = new URLSearchParams({
        pageNumber: String(query.pageNumber ?? 1),
        pageSize: String(query.pageSize ?? 10),
        unreadOnly: String(query.unreadOnly ?? false),
    });

    return apiRequest<PagedResult<NotificationDto>>(
        `/api/notifications?${searchParams.toString()}`,
        { method: "GET", token }
    );
}

export function getUnreadNotificationCount(
    token: string
): Promise<UnreadNotificationCountDto> {
    return apiRequest<UnreadNotificationCountDto>(
        "/api/notifications/unread-count",
        { method: "GET", token }
    );
}

export function markNotificationRead(
    token: string,
    id: string
): Promise<void> {
    return apiRequest<void>(
        `/api/notifications/${id}/read`,
        { method: "PATCH", token }
    );
}

export function markAllNotificationsRead(token: string): Promise<void> {
    return apiRequest<void>(
        "/api/notifications/read-all",
        { method: "PATCH", token }
    );
}
