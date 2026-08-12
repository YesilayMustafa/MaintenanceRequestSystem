import { NavLink } from "react-router-dom";

import { useAuth } from "../auth/useAuth";

export function AppNavigation() {
    const { user } = useAuth();

    return (
        <nav className="sidebar-nav" aria-label="Ana navigasyon">
            <NavItem to="/dashboard" marker="G" label="Genel Bakış" />
            <NavItem to="/tickets" marker="T" label="Talepler" />
            <NavItem to="/assets" marker="C" label="Cihazlar" />

            {user?.role === "Admin" && (
                <>
                    <p className="sidebar-section-label">Yönetim</p>
                    <NavItem to="/categories" marker="K" label="Kategoriler" />
                    <NavItem to="/departments" marker="D" label="Departmanlar" />
                    <NavItem to="/users" marker="K" label="Kullanıcılar" />
                    <NavItem
                        to="/audit-logs"
                        marker="A"
                        label="Audit Logları"
                    />
                </>
            )}

            {user?.role !== "Admin" && (
                <NavItem to="/departments" marker="D" label="Departmanlar" />
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
