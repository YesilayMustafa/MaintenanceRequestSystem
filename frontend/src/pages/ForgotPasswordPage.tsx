import { useState, type SubmitEvent } from "react";
import { Link } from "react-router-dom";

import { forgotPassword } from "../api/authApi";
import { ApiError } from "../api/httpClient";

export function ForgotPasswordPage() {
    const [email, setEmail] = useState("");
    const [message, setMessage] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        if (isSubmitting) {
            return;
        }

        try {
            setError(null);
            setMessage(null);
            setIsSubmitting(true);
            const response = await forgotPassword({ email: email.trim() });
            setMessage(response.message);
        } catch (error) {
            setError(error instanceof ApiError
                ? error.message
                : "Şifre sıfırlama isteği gönderilemedi.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <main className="login-page">
            <section className="login-card" aria-labelledby="forgot-title">
                <div className="login-brand">
                    <span className="login-brand-mark" aria-hidden="true">MRS</span>
                    <h1 id="forgot-title">Şifremi Unuttum</h1>
                    <p>Şifre sıfırlama bağlantısı için e-posta adresinizi girin.</p>
                </div>

                <form className="login-form" onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label htmlFor="forgot-email">E-posta</label>
                        <input
                            id="forgot-email"
                            type="email"
                            autoComplete="email"
                            value={email}
                            onChange={(event) => setEmail(event.target.value)}
                            disabled={isSubmitting}
                            required
                        />
                    </div>

                    {error && <p className="error-state" role="alert">{error}</p>}
                    {message && <p className="success-state" role="status">{message}</p>}

                    <button
                        type="submit"
                        className="button button-primary"
                        disabled={isSubmitting}
                    >
                        {isSubmitting ? "Gönderiliyor..." : "Sıfırlama Bağlantısı Gönder"}
                    </button>

                    <Link className="auth-link" to="/login">Giriş ekranına dön</Link>
                </form>
            </section>
        </main>
    );
}
