import { apiRequest } from "./httpClient";

import type {
    TicketTimelineItemDto,
    TicketTimelineQuery,
} from "../types/timeline";

export function getTicketTimeline(
    token: string,
    query: TicketTimelineQuery
): Promise<TicketTimelineItemDto[]> {
    const searchParams = new URLSearchParams({
        from: query.from,
        to: query.to,
    });

    if (query.status !== undefined) {
        searchParams.set("status", String(query.status));
    }
    if (query.priority !== undefined) {
        searchParams.set("priority", String(query.priority));
    }
    if (query.slaStatus) {
        searchParams.set("slaStatus", query.slaStatus);
    }
    if (query.categoryId) {
        searchParams.set("categoryId", query.categoryId);
    }
    if (query.assignedTechnicianId) {
        searchParams.set(
            "assignedTechnicianId",
            query.assignedTechnicianId
        );
    }
    if (query.departmentId) {
        searchParams.set("departmentId", query.departmentId);
    }

    return apiRequest<TicketTimelineItemDto[]>(
        `/api/tickets/timeline?${searchParams.toString()}`,
        { method: "GET", token }
    );
}
