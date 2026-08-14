import {
    apiRequest,
    apiResponse,
} from "./httpClient";
import {
    getResponseFileName,
    sanitizeFileName,
} from "./attachmentsApi";

import type {
    ReportDownload,
    ReportFilterQuery,
    ReportOverviewDto,
} from "../types/reports";

export function getReportOverview(
    token: string,
    query: ReportFilterQuery = {}
): Promise<ReportOverviewDto> {
    return apiRequest<ReportOverviewDto>(
        `/api/reports/overview${buildQuery(query)}`,
        { method: "GET", token }
    );
}

export async function downloadTicketReport(
    token: string,
    query: ReportFilterQuery = {}
): Promise<ReportDownload> {
    const response = await apiResponse(
        `/api/reports/tickets/export.csv${buildQuery(query)}`,
        { method: "GET", token }
    );
    const responseFileName = getResponseFileName(
        response.headers.get("Content-Disposition")
    );

    return {
        blob: await response.blob(),
        fileName: sanitizeFileName(responseFileName ?? "ticket-report.csv"),
    };
}

function buildQuery(query: ReportFilterQuery): string {
    const parameters = new URLSearchParams();

    Object.entries(query).forEach(([key, value]) => {
        if (value) {
            parameters.set(key, value);
        }
    });

    const value = parameters.toString();
    return value ? `?${value}` : "";
}
