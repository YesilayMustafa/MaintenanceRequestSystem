import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "../auth/ProtectedRoute";
import { RoleRoute } from "../auth/RoleRoute";
import { AppLayout } from "../components/layout/AppLayout";
import { AuditLogsPage } from "../pages/AuditLogsPage";
import { AssetsPage } from "../pages/AssetsPage";
import { DepartmentsPage } from "../pages/DepartmentsPage";
import { LoginPage } from "../pages/LoginPage";
import { CreateTicketPage } from "../pages/CreateTicketPage";
import { TicketsPage } from "../pages/TicketsPage";
import { TicketDetailsPage } from "../pages/TicketDetailsPage";
import { UsersPage } from "../pages/UsersPage";

const adminRoles = ["Admin"] as const;

export function AppRoutes() {
    return (
        <Routes>
            <Route
                path="/"
                element={<Navigate to="/tickets" replace />}
            />

            <Route
                path="/login"
                element={<LoginPage />}
            />

            <Route
                element={
                    <ProtectedRoute>
                        <AppLayout />
                    </ProtectedRoute>
                }
            >
                <Route path="/assets" element={<AssetsPage />} />
                <Route path="/departments" element={<DepartmentsPage />} />
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
                <Route path="/tickets" element={<TicketsPage />} />
                <Route path="/tickets/new" element={<CreateTicketPage />} />
                <Route path="/tickets/:id" element={<TicketDetailsPage />} />
            </Route>

            <Route
                path="*"
                element={<Navigate to="/tickets" replace />}
            />
        </Routes>
    );
}
