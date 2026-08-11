import { useState, type SubmitEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";

import { resetPassword } from "../api/authApi";
import { ApiError } from "../api/httpClient";
import { validateNewPassword } from "../features/auth/passwordValidation";

export function ResetPasswordPage() {
    const [searchParams] = useSearchParams();
    const token = searchParams.get("token") ?? "";
    const [newPassword, setNewPassword] = useState("");
    const [confirmation, setConfirmation] = useState("");
    const [error, setError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [isComplete, setIsComplete] = useState(false);

    async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        if (isSubmitting || isComplete) {
            return;
        }

        const validationError = validateNewPassword(
            newPassword,
            confirmation
        );

        if (validationError) {
            setError(validationError);
            return;
        }

        try {
            setError(null);
            setIsSubmitting(true);
            await resetPassword({ token, newPassword });
            setNewPassword("");
            setConfirmation("");
            setIsComplete(true);
        } catch (error) {
            setError(error instanceof ApiError
                ? error.message
                : "Şifre sıfırlanırken beklenmeyen bir hata oluştu.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <main className="login-page">
            <section className="login-card" aria-labelledby="reset-title">
                <div className="login-brand">
                    <span className="login-brand-mark" aria-hidden="true">M</span>
                    <h1 id="reset-title">Yeni Şifre Belirleyin</h1>
                    <p>Hesabınız için yeni ve güvenli bir parola oluşturun.</p>
                </div>

                {!token && (
                    <p className="error-state" role="alert">
                        Şifre sıfırlama bağlantısı geçersiz veya eksik.
                    </p>
                )}

                {isComplete && (
                    <div className="auth-result" role="status">
                        <p className="success-state">
                            Şifreniz başarıyla değiştirildi.
                        </p>
                        <Link className="button button-primary" to="/login">
                            Girişe Git
                        </Link>
                    </div>
                )}

                {token && !isComplete && (
                    <form className="login-form" onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label htmlFor="reset-password">Yeni Parola</label>
                            <input
                                id="reset-password"
                                type="password"
                                minLength={8}
                                maxLength={128}
                                autoComplete="new-password"
                                value={newPassword}
                                onChange={(event) => setNewPassword(event.target.value)}
                                disabled={isSubmitting}
                                required
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="reset-password-confirmation">
                                Yeni Parola Tekrar
                            </label>
                            <input
                                id="reset-password-confirmation"
                                type="password"
                                minLength={8}
                                maxLength={128}
                                autoComplete="new-password"
                                value={confirmation}
                                onChange={(event) => setConfirmation(event.target.value)}
                                disabled={isSubmitting}
                                required
                            />
                        </div>

                        {error && <p className="error-state" role="alert">{error}</p>}

                        <button
                            type="submit"
                            className="button button-primary"
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? "Değiştiriliyor..." : "Şifreyi Değiştir"}
                        </button>
                    </form>
                )}
            </section>
        </main>
    );
}
