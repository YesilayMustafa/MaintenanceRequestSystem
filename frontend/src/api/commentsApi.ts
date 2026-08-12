import { apiRequest } from "./httpClient";

import type {
    CreateTicketCommentRequest,
    TicketCommentDto,
} from "../types/comments";

export function getTicketComments(
    token: string,
    ticketId: string
): Promise<TicketCommentDto[]> {
    return apiRequest<TicketCommentDto[]>(
        `/api/tickets/${ticketId}/comments`,
        {
            method: "GET",
            token,
        }
    );
}

export function createTicketComment(
    token: string,
    ticketId: string,
    request: CreateTicketCommentRequest
): Promise<TicketCommentDto> {
    return apiRequest<TicketCommentDto>(
        `/api/tickets/${ticketId}/comments`,
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}
