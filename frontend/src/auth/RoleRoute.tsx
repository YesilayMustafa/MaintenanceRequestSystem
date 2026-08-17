import type { ReactNode } from "react";
import { Navigate, useLocation } from "react-router-dom";

import { useAuth } from "./useAuth";

import type { UserRole } from "../types/auth";

interface RoleRouteProps {
    allowedRoles: UserRole[];
    children: ReactNode;
}

export function RoleRoute({
    allowedRoles,
    children,
}: RoleRouteProps) {
    const { user, isAuthenticated, isLoading } = useAuth();
    const location = useLocation();

    if (isLoading) {
        return <p>Oturum kontrol ediliyor...</p>;
    }

    if (!isAuthenticated) {
        return (
            <Navigate
                to="/login"
                replace
                state={{ from: location }}
            />
        );
    }

    if (!user || !allowedRoles.includes(user.role)) {
        return <Navigate to="/forbidden" replace />;
    }

    return children;
}
