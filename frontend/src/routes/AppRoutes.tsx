import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "../auth/ProtectedRoute";
import { RoleRoute } from "../auth/RoleRoute";
import { AppLayout } from "../components/layout/AppLayout";
import { AuditLogsPage } from "../pages/AuditLogsPage";
import { CategoriesPage } from "../pages/CategoriesPage";
import { AcceptInvitationPage } from "../pages/AcceptInvitationPage";
import { AssetsPage } from "../pages/AssetsPage";
import { DepartmentsPage } from "../pages/DepartmentsPage";
import { DashboardPage } from "../pages/DashboardPage";
import { ForgotPasswordPage } from "../pages/ForgotPasswordPage";
import { LoginPage } from "../pages/LoginPage";
import { ProfilePage } from "../pages/ProfilePage";
import { ResetPasswordPage } from "../pages/ResetPasswordPage";
import { CreateTicketPage } from "../pages/CreateTicketPage";
import { TicketsPage } from "../pages/TicketsPage";
import { TicketDetailsPage } from "../pages/TicketDetailsPage";
import { UsersPage } from "../pages/UsersPage";
import { NotificationsPage } from "../pages/NotificationsPage";
import { AssetMaintenanceHistoryPage } from "../pages/AssetMaintenanceHistoryPage";
import { ReportsPage } from "../pages/ReportsPage";

const adminRoles = ["Admin"] as const;

export function AppRoutes() {
    return (
        <Routes>
            <Route
                path="/"
                element={<Navigate to="/dashboard" replace />}
            />

            <Route
                path="/login"
                element={<LoginPage />}
            />
            <Route
                path="/forgot-password"
                element={<ForgotPasswordPage />}
            />
            <Route
                path="/accept-invitation"
                element={<AcceptInvitationPage />}
            />
            <Route
                path="/reset-password"
                element={<ResetPasswordPage />}
            />

            <Route
                element={
                    <ProtectedRoute>
                        <AppLayout />
                    </ProtectedRoute>
                }
            >
                <Route path="/dashboard" element={<DashboardPage />} />
                <Route path="/assets" element={<AssetsPage />} />
                <Route
                    path="/assets/:id/history"
                    element={<AssetMaintenanceHistoryPage />}
                />
                <Route path="/notifications" element={<NotificationsPage />} />
                <Route path="/departments" element={<DepartmentsPage />} />
                <Route path="/profile" element={<ProfilePage />} />
                <Route
                    path="/categories"
                    element={
                        <RoleRoute allowedRoles={[...adminRoles]}>
                            <CategoriesPage />
                        </RoleRoute>
                    }
                />
                <Route
                    path="/users"
                    element={
                        <RoleRoute allowedRoles={[...adminRoles]}>
                            <UsersPage />
                        </RoleRoute>
                    }
                />
                <Route
                    path="/audit-logs"
                    element={
                        <RoleRoute allowedRoles={[...adminRoles]}>
                            <AuditLogsPage />
                        </RoleRoute>
                    }
                />
                <Route
                    path="/reports"
                    element={
                        <RoleRoute allowedRoles={[...adminRoles]}>
                            <ReportsPage />
                        </RoleRoute>
                    }
                />
                <Route path="/tickets" element={<TicketsPage />} />
                <Route path="/tickets/new" element={<CreateTicketPage />} />
                <Route path="/tickets/:id" element={<TicketDetailsPage />} />
            </Route>

            <Route
                path="*"
                element={<Navigate to="/dashboard" replace />}
            />
        </Routes>
    );
}
