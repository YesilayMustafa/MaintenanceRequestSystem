import type {
    SlaStatus,
    TicketPriority,
    TicketPriorityValue,
    TicketStatus,
    TicketStatusValue,
} from "./tickets";

export interface TicketTimelineQuery {
    from: string;
    to: string;
    status?: TicketStatusValue;
    priority?: TicketPriorityValue;
    slaStatus?: SlaStatus;
    categoryId?: string;
    assignedTechnicianId?: string;
    departmentId?: string;
}

export interface TicketTimelineItemDto {
    id: string;
    ticketNumber: string;
    title: string;
    status: TicketStatus;
    priority: TicketPriority;
    categoryId: string;
    categoryName: string;
    assignedTechnicianId: string | null;
    assignedTechnicianFullName: string | null;
    createdAt: string;
    slaDueAt: string;
    slaStatus: SlaStatus;
}
