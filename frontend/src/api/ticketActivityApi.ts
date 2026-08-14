import { apiRequest } from "./httpClient";

import type { PagedResult } from "../types/pagination";
import type { TicketActivityDto } from "../types/ticketActivity";

export function getTicketActivity(
    token: string,
    ticketId: string,
    pageNumber = 1,
    pageSize = 20
): Promise<PagedResult<TicketActivityDto>> {
    const query = new URLSearchParams({
        pageNumber: String(pageNumber),
        pageSize: String(pageSize),
    });

    return apiRequest<PagedResult<TicketActivityDto>>(
        `/api/tickets/${ticketId}/activity?${query.toString()}`,
        { method: "GET", token }
    );
}
