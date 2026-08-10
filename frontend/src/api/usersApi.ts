import { apiRequest } from "./httpClient";

import type {
    ChangeUserRoleRequest,
    ChangeUserStatusRequest,
    CreateUserRequest,
    UpdateUserRequest,
    UserDto,
} from "../types/users";

export function getUsers(token: string): Promise<UserDto[]> {
    return apiRequest<UserDto[]>(
        "/api/users",
        {
            method: "GET",
            token,
        }
    );
}

export function createUser(
    token: string,
    request: CreateUserRequest
): Promise<UserDto> {
    return apiRequest<UserDto>(
        "/api/users",
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function updateUser(
    token: string,
    id: string,
    request: UpdateUserRequest
): Promise<UserDto> {
    return apiRequest<UserDto>(
        `/api/users/${id}`,
        {
            method: "PUT",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function changeUserStatus(
    token: string,
    id: string,
    request: ChangeUserStatusRequest
): Promise<void> {
    return apiRequest<void>(
        `/api/users/${id}/status`,
        {
            method: "PATCH",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function changeUserRole(
    token: string,
    id: string,
    request: ChangeUserRoleRequest
): Promise<void> {
    return apiRequest<void>(
        `/api/users/${id}/role`,
        {
            method: "PATCH",
            token,
            body: JSON.stringify(request),
        }
    );
}
