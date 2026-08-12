import type { AssetTypeName } from "./assets";
import type { PagedResult } from "./pagination";
import type {
    TicketPriority,
    TicketStatus,
} from "./tickets";

export interface AssetMaintenanceHistoryDto {
    asset: AssetMaintenanceHistoryAssetDto;
    summary: AssetMaintenanceSummaryDto;
    tickets: PagedResult<AssetMaintenanceTicketDto>;
}

export interface AssetMaintenanceHistoryAssetDto {
    id: string;
    name: string;
    serialNumber: string;
    type: AssetTypeName;
}

export interface AssetMaintenanceSummaryDto {
    totalTicketCount: number;
    activeTicketCount: number;
    resolvedTicketCount: number;
    closedTicketCount: number;
    criticalTicketCount: number;
    lastTicketCreatedAt: string | null;
}

export interface AssetMaintenanceTicketDto {
    id: string;
    ticketNumber: string;
    title: string;
    categoryId: string;
    categoryName: string;
    status: TicketStatus;
    priority: TicketPriority;
    createdAt: string;
    resolvedAt: string | null;
    closedAt: string | null;
    createdByFullName: string;
    assignedTechnicianFullName: string | null;
}
