export type AssetTypeName =
    | "Computer"
    | "Printer"
    | "Server"
    | "NetworkDevice"
    | "SoftwareSystem"
    | "Other";

export interface AssetDto {
    id: string;
    name: string;
    serialNumber: string;
    type: AssetTypeName;
    location: string | null;
    departmentId: string;
    departmentName: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export type AssetTypeValue = 1 | 2 | 3 | 4 | 5 | 6;

export interface CreateAssetRequest {
    name: string;
    serialNumber: string;
    type: AssetTypeValue;
    departmentId: string;
    location: string | null;
}

export type UpdateAssetRequest = CreateAssetRequest;

export interface ChangeAssetStatusRequest {
    isActive: boolean;
}
