import { NavLink } from "react-router-dom";

import { useAuth } from "../auth/useAuth";

export function AppNavigation() {
    const { user } = useAuth();

    return (
        <nav className="sidebar-nav" aria-label="Ana navigasyon">
            <NavItem to="/tickets" marker="T" label="Talepler" />
            <NavItem to="/assets" marker="C" label="Cihazlar" />
            <NavItem to="/departments" marker="D" label="Departmanlar" />

            {user?.role === "Admin" && (
                <>
                    <p className="sidebar-section-label">Yönetim</p>
                    <NavItem to="/users" marker="K" label="Kullanıcılar" />
                    <NavItem
                        to="/audit-logs"
                        marker="A"
                        label="Audit Logları"
                    />
                </>
            )}
        </nav>
    );
}

interface NavItemProps {
    to: string;
    marker: string;
    label: string;
}

function NavItem({ to, marker, label }: NavItemProps) {
    return (
        <NavLink
            to={to}
            className={({ isActive }) =>
                `sidebar-link${isActive ? " sidebar-link-active" : ""}`
            }
        >
            <span className="sidebar-link-marker" aria-hidden="true">
                {marker}
            </span>
            <span>{label}</span>
        </NavLink>
    );
}
