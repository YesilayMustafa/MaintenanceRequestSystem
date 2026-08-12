import { apiRequest } from "./httpClient";

import type { PagedResult } from "../types/pagination";
import type {
    AssignTicketRequest,
    ChangeTicketPriorityRequest,
    CreateTicketRequest,
    PutTicketOnHoldRequest,
    ReopenTicketRequest,
    ResolveTicketRequest,
    TicketDto,
    TicketHistoryDto,
    TicketListQuery,
} from "../types/tickets";

export function createTicket(
    token: string,
    request: CreateTicketRequest
): Promise<TicketDto> {
    return apiRequest<TicketDto>(
        "/api/tickets",
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}

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

    if (query.ticketNumber) {
        searchParams.set("ticketNumber", query.ticketNumber);
    }

    return apiRequest<PagedResult<TicketDto>>(
        `/api/tickets?${searchParams.toString()}`,
        {
            method: "GET",
            token,
        }
    );

}

export function getTicketById(
    token: string,
    id: string
): Promise<TicketDto> {
    return apiRequest<TicketDto>(
        `/api/tickets/${id}`,
        {
            method: "GET",
            token,
        }
    );
}

export function getTicketHistory(
    token: string,
    id: string
): Promise<TicketHistoryDto[]> {
    return apiRequest<TicketHistoryDto[]>(
        `/api/tickets/${id}/history`,
        {
            method: "GET",
            token,
        }
    );
}

export function assignTicket(
    token: string,
    id: string,
    request: AssignTicketRequest
): Promise<TicketDto> {
    return patchTicket(token, id, "assignment", request);
}

export function reassignTicket(
    token: string,
    id: string,
    request: AssignTicketRequest
): Promise<TicketDto> {
    return patchTicket(token, id, "reassignment", request);
}

export function startProgress(
    token: string,
    id: string
): Promise<TicketDto> {
    return patchTicket(token, id, "start-progress");
}

export function putOnHold(
    token: string,
    id: string,
    request: PutTicketOnHoldRequest
): Promise<TicketDto> {
    return patchTicket(token, id, "put-on-hold", request);
}

export function resumeTicket(
    token: string,
    id: string
): Promise<TicketDto> {
    return patchTicket(token, id, "resume");
}

export function resolveTicket(
    token: string,
    id: string,
    request: ResolveTicketRequest
): Promise<TicketDto> {
    return patchTicket(token, id, "resolve", request);
}

export function closeTicket(
    token: string,
    id: string
): Promise<TicketDto> {
    return patchTicket(token, id, "close");
}

export function reopenTicket(
    token: string,
    id: string,
    request: ReopenTicketRequest
): Promise<TicketDto> {
    return patchTicket(token, id, "reopen", request);
}

export function cancelTicket(
    token: string,
    id: string
): Promise<TicketDto> {
    return patchTicket(token, id, "cancel");
}

export function changeTicketPriority(
    token: string,
    id: string,
    request: ChangeTicketPriorityRequest
): Promise<TicketDto> {
    return patchTicket(token, id, "priority", request);
}

export function softDeleteTicket(
    token: string,
    id: string
): Promise<void> {
    return apiRequest<void>(
        `/api/tickets/${id}`,
        {
            method: "DELETE",
            token,
        }
    );
}

function patchTicket(
    token: string,
    id: string,
    action: string,
    request?: object
): Promise<TicketDto> {
    return apiRequest<TicketDto>(
        `/api/tickets/${id}/${action}`,
        {
            method: "PATCH",
            token,
            body: request
                ? JSON.stringify(request)
                : undefined,
        }
    );
}
