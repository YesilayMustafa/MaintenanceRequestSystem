import { apiRequest } from "./httpClient";

import type {
    AssetDto,
    ChangeAssetStatusRequest,
    CreateAssetRequest,
    UpdateAssetRequest,
} from "../types/assets";

export function getAssets(token: string): Promise<AssetDto[]> {
    return apiRequest<AssetDto[]>(
        "/api/assets",
        {
            method: "GET",
            token,
        }
    );
}

export function createAsset(
    token: string,
    request: CreateAssetRequest
): Promise<AssetDto> {
    return apiRequest<AssetDto>(
        "/api/assets",
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function updateAsset(
    token: string,
    id: string,
    request: UpdateAssetRequest
): Promise<AssetDto> {
    return apiRequest<AssetDto>(
        `/api/assets/${id}`,
        {
            method: "PUT",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function changeAssetStatus(
    token: string,
    id: string,
    request: ChangeAssetStatusRequest
): Promise<void> {
    return apiRequest<void>(
        `/api/assets/${id}/status`,
        {
            method: "PATCH",
            token,
            body: JSON.stringify(request),
        }
    );
}
