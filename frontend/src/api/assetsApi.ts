import { apiRequest } from "./httpClient";

import type { AssetDto } from "../types/assets";

export function getAssets(token: string): Promise<AssetDto[]> {
    return apiRequest<AssetDto[]>(
        "/api/assets",
        {
            method: "GET",
            token,
        }
    );
}
