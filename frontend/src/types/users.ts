import type { UserRole } from "./auth";

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
}
