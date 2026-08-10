import { apiRequest } from "./httpClient";

import type {
    ChangeDepartmentStatusRequest,
    CreateDepartmentRequest,
    DepartmentDto,
    UpdateDepartmentRequest,
} from "../types/departments";

export function getDepartments(
    token: string
): Promise<DepartmentDto[]> {
    return apiRequest<DepartmentDto[]>(
        "/api/departments",
        {
            method: "GET",
            token,
        }
    );
}

export function createDepartment(
    token: string,
    request: CreateDepartmentRequest
): Promise<DepartmentDto> {
    return apiRequest<DepartmentDto>(
        "/api/departments",
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function updateDepartment(
    token: string,
    id: string,
    request: UpdateDepartmentRequest
): Promise<DepartmentDto> {
    return apiRequest<DepartmentDto>(
        `/api/departments/${id}`,
        {
            method: "PUT",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function changeDepartmentStatus(
    token: string,
    id: string,
    request: ChangeDepartmentStatusRequest
): Promise<void> {
    return apiRequest<void>(
        `/api/departments/${id}/status`,
        {
            method: "PATCH",
            token,
            body: JSON.stringify(request),
        }
    );
}
