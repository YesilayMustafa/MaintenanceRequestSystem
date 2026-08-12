export interface DepartmentDto {
    id: string;
    name: string;
    description: string | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export interface CreateDepartmentRequest {
    name: string;
    description: string | null;
}

export type UpdateDepartmentRequest = CreateDepartmentRequest;

export interface ChangeDepartmentStatusRequest {
    isActive: boolean;
}
