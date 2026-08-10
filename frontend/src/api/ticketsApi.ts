import { apiRequest } from "./httpClient";

import type { PagedResult } from "../types/pagination";
import type {
    TicketDto,
    TicketListQuery,
} from "../types/tickets";

export function getTickets(
    token: string,
    query: TicketListQuery = {}
): Promise<PagedResult<TicketDto>> {
    const searchParams = new URLSearchParams();

    searchParams.set(
        "pageNumber",
        String(query.pageNumber ?? 1)
    );

    searchParams.set(
        "pageSize",
        String(query.pageSize ?? 10)
    );

    searchParams.set(
        "sortBy",
        query.sortBy ?? "createdAt"
    );

    searchParams.set(
        "sortDescending",
        String(query.sortDescending ?? true)
    );

    if (query.status !== undefined) {
        searchParams.set("status", String(query.status));
    }

    if (query.priority !== undefined) {
        searchParams.set("priority", String(query.priority));
    }

    if (query.assetId) {
        searchParams.set("assetId", query.assetId);
    }

    return apiRequest<PagedResult<TicketDto>>(
        `/api/tickets?${searchParams.toString()}`,
        {
            method: "GET",
            token,
        }
    );
}