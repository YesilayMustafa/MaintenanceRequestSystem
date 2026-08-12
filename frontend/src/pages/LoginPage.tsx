import { Navigate, useLocation, useNavigate } from "react-router-dom";
import { LoginForm } from "../features/auth/LoginForm";
import { useAuth } from "../auth/useAuth";

interface LocationState {
    from?: {
        pathname?: string;
    };
    message?: string;
}

export function LoginPage() {
    const { isAuthenticated, isLoading } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const state = location.state as LocationState | null;
    const returnPath = state?.from?.pathname ?? "/tickets";

    if (isLoading) {
        return (
            <main className="login-page">
                <p className="loading-state">Oturum kontrol ediliyor...</p>
            </main>
        );
    }

    if (isAuthenticated) {
        return <Navigate to="/tickets" replace />;
    }

    function handleLoginSuccess() {
        navigate(returnPath, { replace: true });
    }

    return (
        <main className="login-page">
            <section className="login-card" aria-labelledby="login-title">
                <div className="login-brand">
                    <span className="login-brand-mark" aria-hidden="true">M</span>
                    <h1 id="login-title">Maintenance Desk</h1>
                    <p>
                        Arıza ve bakım taleplerini yönetmek için hesabınızla
                        giriş yapın.
                    </p>
                </div>

                {state?.message && (
                    <p className="success-state" role="status">
                        {state.message}
                    </p>
                )}

                <LoginForm onSuccess={handleLoginSuccess} />
            </section>
        </main>
    );
}
