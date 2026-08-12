import { apiRequest } from "./httpClient";

import type { AssetMaintenanceHistoryDto } from "../types/assetMaintenanceHistory";

export function getAssetMaintenanceHistory(
    token: string,
    assetId: string,
    pageNumber: number,
    pageSize: number
): Promise<AssetMaintenanceHistoryDto> {
    const searchParams = new URLSearchParams({
        pageNumber: String(pageNumber),
        pageSize: String(pageSize),
    });

    return apiRequest<AssetMaintenanceHistoryDto>(
        `/api/assets/${assetId}/maintenance-history?${searchParams.toString()}`,
        { method: "GET", token }
    );
}
