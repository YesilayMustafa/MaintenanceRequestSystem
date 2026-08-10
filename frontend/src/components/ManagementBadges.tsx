import type { UserRole } from "../types/auth";

interface ActiveStatusBadgeProps {
    isActive: boolean;
}

export function ActiveStatusBadge({ isActive }: ActiveStatusBadgeProps) {
    return (
        <span
            className={
                `badge management-status-badge ` +
                (isActive ? "badge-active" : "badge-inactive")
            }
        >
            {isActive ? "Aktif" : "Pasif"}
        </span>
    );
}

interface UserRoleBadgeProps {
    role: UserRole;
}

export function UserRoleBadge({ role }: UserRoleBadgeProps) {
    return (
        <span className={`badge role-badge role-${role.toLowerCase()}`}>
            {role}
        </span>
    );
}
