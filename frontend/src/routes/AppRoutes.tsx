import { Navigate, Route, Routes } from "react-router-dom";
import { ProtectedRoute } from "../auth/ProtectedRoute";
import { LoginPage } from "../pages/LoginPage";
import { TicketsPage } from "../pages/TicketsPage";
import { TicketDetailsPage } from "../pages/TicketDetailsPage";

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
                path="/tickets"
                element={
                    <ProtectedRoute>
                        <TicketsPage />
                    </ProtectedRoute>
                }
            />

            <Route
                path="/tickets/:id"
                element={
                    <ProtectedRoute>
                        <TicketDetailsPage />
                    </ProtectedRoute>
                }
            />

            <Route
                path="*"
                element={<Navigate to="/tickets" replace />}
            />
        </Routes>
    );
}