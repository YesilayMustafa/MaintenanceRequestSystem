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

export interface TicketDto {
    id: string;
    title: string;
    description: string;

    priority: TicketPriority;
    status: TicketStatus;

    assetId: string;
    assetName: string;
    assetSerialNumber: string;

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
}

export interface TicketListQuery {
    pageNumber?: number;
    pageSize?: number;
    status?: number;
    priority?: number;
    assetId?: string;
    sortBy?: "createdAt" | "title" | "priority" | "status";
    sortDescending?: boolean;
}