import type { AccountStatus, UserRole } from "../types/auth";

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

interface AccountStatusBadgeProps {
    status: AccountStatus;
}

const accountStatusLabels: Record<AccountStatus, string> = {
    Active: "Aktif",
    PendingInvitation: "Davet Bekliyor",
    Inactive: "Pasif",
};

export function AccountStatusBadge({ status }: AccountStatusBadgeProps) {
    return (
        <span
            className={
                `badge management-status-badge ` +
                `badge-account-${status.toLowerCase()}`
            }
        >
            {accountStatusLabels[status]}
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
