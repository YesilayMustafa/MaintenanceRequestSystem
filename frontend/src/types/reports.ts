export interface ReportFilterQuery {
    createdFrom?: string;
    createdTo?: string;
    categoryId?: string;
    departmentId?: string;
    assignedTechnicianId?: string;
}

export interface ReportSummaryDto {
    totalTickets: number;
    activeTickets: number;
    resolvedTickets: number;
    closedTickets: number;
    cancelledTickets: number;
    criticalTickets: number;
    slaMetCount: number;
    slaBreachedCount: number;
    slaComplianceRate: number;
}

export interface ReportDistributionItemDto {
    key: string;
    label: string;
    count: number;
}

export interface ReportTrendItemDto {
    date: string;
    count: number;
}

export interface TechnicianPerformanceDto {
    technicianId: string;
    fullName: string;
    assignedCount: number;
    activeCount: number;
    resolvedOrClosedCount: number;
    slaMetCount: number;
    slaBreachedCount: number;
    slaComplianceRate: number;
}

export interface ReportOverviewDto {
    summary: ReportSummaryDto;
    byStatus: ReportDistributionItemDto[];
    byPriority: ReportDistributionItemDto[];
    byCategory: ReportDistributionItemDto[];
    dailyCreationTrend: ReportTrendItemDto[];
    technicianPerformance: TechnicianPerformanceDto[];
}

export interface ReportDownload {
    blob: Blob;
    fileName: string;
}
