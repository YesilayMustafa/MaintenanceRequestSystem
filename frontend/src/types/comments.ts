export interface TicketCommentDto {
    id: string;
    ticketId: string;
    userId: string;
    userFullName: string;
    userRole: string;
    content: string;
    createdAt: string;
}

export interface CreateTicketCommentRequest {
    content: string;
}