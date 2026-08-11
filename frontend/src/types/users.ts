import type { AccountStatus, UserRole } from "./auth";

export interface UserDto {
    id: string;
    fullName: string;
    email: string;
    role: UserRole;
    departmentId: string;
    departmentName: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
    accountStatus: AccountStatus;
}

export type UserRoleValue = 1 | 2 | 3;

export interface InviteUserRequest {
    fullName: string;
    email: string;
    role: UserRoleValue;
    departmentId: string;
}

export interface UpdateUserRequest {
    fullName: string;
    email: string;
    departmentId: string;
}

export interface ChangeUserStatusRequest {
    isActive: boolean;
}

export interface ChangeUserRoleRequest {
    role: UserRoleValue;
}
