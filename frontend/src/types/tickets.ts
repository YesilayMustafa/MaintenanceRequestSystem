export type TicketStatus =
    | "Open"
    | "Assigned"
    | "InProgress"
    | "Waiting"
    | "Resolved"
    | "Closed"
    | "Cancelled";

export type TicketPriority =
    | "Low"
    | "Medium"
    | "High"
    | "Critical";

export type SlaStatus =
    | "OnTrack"
    | "DueSoon"
    | "Breached"
    | "Met"
    | "NotApplicable";

export type TicketPriorityValue = 1 | 2 | 3 | 4;

export type TicketStatusValue = 1 | 2 | 3 | 4 | 5 | 6 | 7;

export type TicketSortBy =
    | "createdAt"
    | "title"
    | "priority"
    | "status"
    | "ticketNumber"
    | "category";

export interface TicketDto {
    id: string;
    ticketNumber: string;
    title: string;
    description: string;

    priority: TicketPriority;
    status: TicketStatus;

    assetId: string;
    assetName: string;
    assetSerialNumber: string;

    categoryId: string;
    categoryName: string;

    createdByUserId: string;
    createdByFullName: string;

    assignedTechnicianId: string | null;
    assignedTechnicianFullName: string | null;

    waitingReason: string | null;
    resolutionDescription: string | null;

    createdAt: string;
    updatedAt: string | null;
    resolvedAt: string | null;
    closedAt: string | null;

    slaDueAt: string;
    slaStatus: SlaStatus;
    slaRemainingMinutes: number | null;
}

export interface TicketHistoryDto {
    id: string;
    performedByUserId: string;
    oldStatus: string | null;
    newStatus: string;
    description: string;
    occurredAt: string;
}

export interface AssignTicketRequest {
    technicianId: string;
}

export interface PutTicketOnHoldRequest {
    reason: string;
}

export interface ResolveTicketRequest {
    resolutionDescription: string;
}

export interface ReopenTicketRequest {
    reason: string;
}

export interface ChangeTicketPriorityRequest {
    priority: TicketPriorityValue;
}

export interface ChangeTicketCategoryRequest {
    categoryId: string;
}

export interface CreateTicketRequest {
    assetId: string;
    categoryId: string;
    title: string;
    description: string;
    priority: TicketPriorityValue;
}

export interface TicketListQuery {
    pageNumber?: number;
    pageSize?: number;
    status?: TicketStatusValue;
    priority?: TicketPriorityValue;
    slaStatus?: SlaStatus;
    assetId?: string;
    ticketNumber?: string;
    search?: string;
    categoryId?: string;
    createdByUserId?: string;
    assignedTechnicianId?: string;
    departmentId?: string;
    createdFrom?: string;
    createdTo?: string;
    sortBy?: TicketSortBy;
    sortDescending?: boolean;
}
