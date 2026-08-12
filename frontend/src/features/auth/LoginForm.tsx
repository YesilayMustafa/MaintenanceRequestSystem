import { useState, type SubmitEvent } from "react";
import { Link } from "react-router-dom";
import { ApiError } from "../../api/httpClient";
import { useAuth } from "../../auth/useAuth";

interface LoginFormProps {
    onSuccess?: () => void;
}

export function LoginForm({ onSuccess }: LoginFormProps) {
    const { login } = useAuth();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        setError(null);
        setIsSubmitting(true);

        try {
            await login({
                email: email.trim(),
                password,
            });

            onSuccess?.();
        } catch (error) {
            if (error instanceof ApiError) {
                setError(error.message);
            } else {
                setError("Giriş sırasında beklenmeyen bir hata oluştu.");
            }
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <form className="login-form" onSubmit={handleSubmit}>
            <div className="form-group">
                <label htmlFor="email">E-posta</label>
                <input
                    id="email"
                    type="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    autoComplete="email"
                    required
                />
            </div>

            <div className="form-group">
                <label htmlFor="password">Şifre</label>
                <input
                    id="password"
                    type="password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    autoComplete="current-password"
                    required
                />
            </div>

            {error && (
                <p className="error-state" role="alert">{error}</p>
            )}

            <button
                type="submit"
                className="button button-primary"
                disabled={isSubmitting}
            >
                {isSubmitting ? "Giriş yapılıyor..." : "Giriş Yap"}
            </button>

            <Link className="auth-link" to="/forgot-password">
                Şifremi Unuttum
            </Link>
        </form>
    );
}
