export interface AssetDto {
    id: string;
    name: string;
    serialNumber: string;
    type: string;
    location: string | null;
    departmentId: string;
    departmentName: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}
