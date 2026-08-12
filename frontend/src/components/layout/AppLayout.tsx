import {
    Link,
    Outlet,
    useLocation,
    useNavigate,
} from "react-router-dom";

import { useAuth } from "../../auth/useAuth";
import { AppNavigation } from "../AppNavigation";
import { NotificationCenter } from "../../features/notifications/NotificationCenter";

const pageTitles: Array<{
    path: string;
    title: string;
}> = [
    { path: "/dashboard", title: "Genel Bakış" },
    { path: "/tickets", title: "Talep Yönetimi" },
    { path: "/assets", title: "Cihaz Yönetimi" },
    { path: "/categories", title: "Kategori Yönetimi" },
    { path: "/departments", title: "Departman Yönetimi" },
    { path: "/users", title: "Kullanıcı Yönetimi" },
    { path: "/audit-logs", title: "Sistem Kayıtları" },
    { path: "/profile", title: "Profil" },
    { path: "/notifications", title: "Bildirimler" },
];

export function AppLayout() {
    const { user, token, logout } = useAuth();
    const location = useLocation();
    const navigate = useNavigate();

    const pageTitle = pageTitles.find(({ path }) =>
        location.pathname.startsWith(path)
    )?.title ?? "Service Desk";

    function handleLogout() {
        logout();
        navigate("/login", { replace: true });
    }

    return (
        <div className="app-shell">
            <aside className="sidebar">
                <div className="brand">
                    <span className="brand-mark" aria-hidden="true">M</span>
                    <div>
                        <strong>Maintenance Desk</strong>
                        <span>IT Service Management</span>
                    </div>
                </div>

                <AppNavigation />
            </aside>

            <div className="app-main">
                <header className="app-header">
                    <div>
                        <span className="header-eyebrow">MaintenanceRequestSystem</span>
                        <strong className="header-title">{pageTitle}</strong>
                    </div>

                    <div className="user-menu">
                        {token && <NotificationCenter token={token} />}
                        <div className="user-avatar" aria-hidden="true">
                            {getInitials(user?.fullName)}
                        </div>
                        <div className="user-summary">
                            <strong>{user?.fullName}</strong>
                            <span>{user?.role}</span>
                        </div>
                        <Link
                            className="button button-secondary button-small"
                            to="/profile"
                        >
                            Profil
                        </Link>
                        <button
                            type="button"
                            className="button button-secondary button-small"
                            onClick={handleLogout}
                        >
                            Çıkış Yap
                        </button>
                    </div>
                </header>

                <main className="page-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}

function getInitials(fullName: string | undefined): string {
    if (!fullName) {
        return "?";
    }

    return fullName
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part[0])
        .join("")
        .toLocaleUpperCase("tr-TR");
}
