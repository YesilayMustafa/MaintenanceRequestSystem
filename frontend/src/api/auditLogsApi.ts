import { apiRequest } from "./httpClient";

import type {
    AuditLogDto,
    AuditLogListQuery,
} from "../types/audit";
import type { PagedResult } from "../types/pagination";

export function getAuditLogs(
    token: string,
    query: AuditLogListQuery
): Promise<PagedResult<AuditLogDto>> {
    const searchParams = new URLSearchParams({
        pageNumber: query.pageNumber.toString(),
        pageSize: query.pageSize.toString(),
    });

    addOptionalParameter(
        searchParams,
        "performedByUserId",
        query.performedByUserId
    );
    addOptionalParameter(searchParams, "action", query.action);
    addOptionalParameter(searchParams, "entityName", query.entityName);
    addOptionalParameter(searchParams, "entityId", query.entityId);
    addOptionalParameter(searchParams, "startDate", query.startDate);
    addOptionalParameter(searchParams, "endDate", query.endDate);

    return apiRequest<PagedResult<AuditLogDto>>(
        `/api/audit-logs?${searchParams.toString()}`,
        {
            method: "GET",
            token,
        }
    );
}

function addOptionalParameter(
    searchParams: URLSearchParams,
    name: string,
    value: string | undefined
) {
    if (value) {
        searchParams.set(name, value);
    }
}
