import type {
    TicketPriority,
    TicketStatus,
} from "./tickets";

export interface DashboardTicketDto {
    id: string;
    ticketNumber: string;
    title: string;
    status: TicketStatus;
    priority: TicketPriority;
    assetName: string;
    createdAt: string;
    assignedTechnicianFullName: string | null;
}

export interface TechnicianWorkloadDto {
    technicianId: string;
    fullName: string;
    activeTicketCount: number;
}

export interface AdminDashboardDto {
    unassignedOpenCount: number;
    technicianWorkload: TechnicianWorkloadDto[];
}

export interface DashboardDto {
    totalCount: number;
    activeCount: number;
    openCount: number;
    assignedCount: number;
    inProgressCount: number;
    waitingCount: number;
    resolvedCount: number;
    closedCount: number;
    cancelledCount: number;
    criticalActiveCount: number;
    recentTickets: DashboardTicketDto[];
    admin: AdminDashboardDto | null;
}
