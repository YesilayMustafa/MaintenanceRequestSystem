import { useEffect, useState } from "react";
import {
    Link,
    Outlet,
    useLocation,
    useNavigate,
} from "react-router-dom";

import { useAuth } from "../../auth/useAuth";
import { AppNavigation } from "../AppNavigation";
import { NotificationCenter } from "../../features/notifications/NotificationCenter";
import { Icon } from "../Icon";

const pageTitles: Array<{
    path: string;
    title: string;
}> = [
    { path: "/dashboard", title: "Genel Bakış" },
    { path: "/tickets/new", title: "Yeni Talep" },
    { path: "/tickets/", title: "Talep Detayı" },
    { path: "/tickets", title: "Talep Yönetimi" },
    { path: "/timeline", title: "Talep Zaman Çizelgesi" },
    { path: "/assets/", title: "Bakım Geçmişi" },
    { path: "/assets", title: "Cihaz Yönetimi" },
    { path: "/categories", title: "Kategori Yönetimi" },
    { path: "/departments", title: "Departman Yönetimi" },
    { path: "/users", title: "Kullanıcı Yönetimi" },
    { path: "/audit-logs", title: "Sistem Kayıtları" },
    { path: "/reports", title: "Raporlar" },
    { path: "/profile", title: "Profil" },
    { path: "/notifications", title: "Bildirimler" },
];

export function AppLayout() {
    const { user, token, logout } = useAuth();
    const location = useLocation();
    const navigate = useNavigate();
    const [isNavigationOpen, setIsNavigationOpen] = useState(false);

    const pageTitle = pageTitles.find(({ path }) =>
        location.pathname.startsWith(path)
    )?.title ?? "Service Desk";

    useEffect(() => {
        setIsNavigationOpen(false);
    }, [location.pathname]);

    function handleLogout() {
        logout();
        navigate("/login", { replace: true });
    }

    return (
        <div className="app-shell">
            <button
                type="button"
                className={`sidebar-backdrop${isNavigationOpen ? " sidebar-backdrop-visible" : ""}`}
                aria-label="Navigasyonu kapat"
                onClick={() => setIsNavigationOpen(false)}
            />
            <aside className={`sidebar${isNavigationOpen ? " sidebar-open" : ""}`}>
                <div className="brand">
                    <span className="brand-mark" aria-hidden="true">MRS</span>
                    <div>
                        <strong>Maintenance Desk</strong>
                        <span>Bakım Talep Yönetimi</span>
                    </div>
                    <button
                        type="button"
                        className="sidebar-close"
                        aria-label="Navigasyonu kapat"
                        onClick={() => setIsNavigationOpen(false)}
                    >
                        <Icon name="close" />
                    </button>
                </div>

                <AppNavigation onNavigate={() => setIsNavigationOpen(false)} />

                <div className="sidebar-footer">
                    <Link className="sidebar-profile" to="/profile">
                        <span className="user-avatar user-avatar-dark" aria-hidden="true">
                            {getInitials(user?.fullName)}
                        </span>
                        <span>
                            <strong>{user?.fullName}</strong>
                            <small>{user?.role}</small>
                        </span>
                    </Link>
                    <button
                        type="button"
                        className="sidebar-logout"
                        aria-label="Çıkış yap"
                        title="Çıkış yap"
                        onClick={handleLogout}
                    >
                        <Icon name="logout" />
                    </button>
                </div>
            </aside>

            <div className="app-main">
                <header className="app-header">
                    <div className="header-leading">
                        <button
                            type="button"
                            className="mobile-menu-button"
                            aria-label="Navigasyonu aç"
                            aria-expanded={isNavigationOpen}
                            onClick={() => setIsNavigationOpen(true)}
                        >
                            <Icon name="menu" />
                        </button>
                        <div>
                            <span className="header-eyebrow">Maintenance Desk</span>
                            <strong className="header-title">{pageTitle}</strong>
                        </div>
                    </div>

                    <div className="user-menu">
                        {token && <NotificationCenter token={token} />}
                        <Link
                            className="header-profile-link"
                            to="/profile"
                            aria-label="Profili aç"
                        >
                            <div className="user-avatar" aria-hidden="true">
                                {getInitials(user?.fullName)}
                            </div>
                            <div className="user-summary">
                                <strong>{user?.fullName}</strong>
                                <span>{user?.role}</span>
                            </div>
                        </Link>
                        <button
                            type="button"
                            className="header-logout"
                            aria-label="Çıkış yap"
                            title="Çıkış yap"
                            onClick={handleLogout}
                        >
                            <Icon name="logout" />
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
