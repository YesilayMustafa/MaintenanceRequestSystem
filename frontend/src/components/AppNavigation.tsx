import { NavLink } from "react-router-dom";

import { useAuth } from "../auth/useAuth";
import { Icon, type IconName } from "./Icon";

interface AppNavigationProps {
    onNavigate?: () => void;
}

export function AppNavigation({ onNavigate }: AppNavigationProps) {
    const { user } = useAuth();

    return (
        <nav className="sidebar-nav" aria-label="Ana navigasyon">
            <p className="sidebar-section-label">Genel</p>
            <NavItem to="/dashboard" icon="home" label="Genel Bakış" onNavigate={onNavigate} />
            <NavItem to="/tickets" icon="ticket" label="Talepler" onNavigate={onNavigate} />
            <NavItem to="/timeline" icon="calendar" label="Takvim" onNavigate={onNavigate} />
            <NavItem to="/assets" icon="asset" label="Cihazlar" onNavigate={onNavigate} />
            <NavItem to="/notifications" icon="bell" label="Bildirimler" onNavigate={onNavigate} />

            {user?.role === "Admin" && (
                <>
                    <p className="sidebar-section-label">Yönetim</p>
                    <NavItem to="/reports" icon="chart" label="Raporlar" onNavigate={onNavigate} />
                    <NavItem to="/users" icon="users" label="Kullanıcılar" onNavigate={onNavigate} />
                    <NavItem to="/departments" icon="building" label="Departmanlar" onNavigate={onNavigate} />
                    <NavItem to="/categories" icon="category" label="Kategoriler" onNavigate={onNavigate} />
                    <NavItem to="/audit-logs" icon="audit" label="Audit Kayıtları" onNavigate={onNavigate} />
                </>
            )}

            {user?.role !== "Admin" && (
                <>
                    <p className="sidebar-section-label">Organizasyon</p>
                    <NavItem to="/departments" icon="building" label="Departmanlar" onNavigate={onNavigate} />
                </>
            )}
        </nav>
    );
}

interface NavItemProps {
    to: string;
    icon: IconName;
    label: string;
    onNavigate?: () => void;
}

function NavItem({ to, icon, label, onNavigate }: NavItemProps) {
    return (
        <NavLink
            to={to}
            onClick={onNavigate}
            className={({ isActive }) =>
                `sidebar-link${isActive ? " sidebar-link-active" : ""}`
            }
        >
            <Icon name={icon} className="sidebar-link-icon" />
            <span>{label}</span>
        </NavLink>
    );
}
