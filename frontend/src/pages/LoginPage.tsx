import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { LoginForm } from "../features/auth/LoginForm";
import { useAuth } from "../auth/useAuth";

interface LocationState {
    from?: {
        pathname?: string;
    };
}

export function LoginPage() {
    const { isAuthenticated, isLoading } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const state = location.state as LocationState | null;
    const returnPath = state?.from?.pathname ?? "/tickets";

    if (isLoading) {
        return <p>Oturum kontrol ediliyor...</p>;
    }

    if (isAuthenticated) {
        return <Navigate to="/tickets" replace />;
    }

    function handleLoginSuccess() {
        navigate(returnPath, { replace: true });
    }

    return (
        <main>
            <h1>Arıza ve Bakım Talep Yönetim Sistemi</h1>

            <p>Devam etmek için hesabınızla giriş yapın.</p>

            <LoginForm onSuccess={handleLoginSuccess} />
        </main>
    );
}