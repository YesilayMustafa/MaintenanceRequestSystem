import { apiRequest } from "./httpClient";

import type {
    ChangeUserRoleRequest,
    ChangeUserStatusRequest,
    InviteUserRequest,
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

export function inviteUser(
    token: string,
    request: InviteUserRequest
): Promise<UserDto> {
    return apiRequest<UserDto>(
        "/api/users/invitations",
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function resendInvitation(
    token: string,
    userId: string
): Promise<void> {
    return apiRequest<void>(
        `/api/users/${userId}/invitations/resend`,
        {
            method: "POST",
            token,
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
