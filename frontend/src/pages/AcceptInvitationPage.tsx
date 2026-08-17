import { useState, type SubmitEvent } from "react";
import { Link, useSearchParams } from "react-router-dom";

import { acceptInvitation } from "../api/authApi";
import { ApiError } from "../api/httpClient";
import { validateNewPassword } from "../features/auth/passwordValidation";

export function AcceptInvitationPage() {
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
            await acceptInvitation({ token, newPassword });
            setNewPassword("");
            setConfirmation("");
            setIsComplete(true);
        } catch (error) {
            setError(error instanceof ApiError
                ? error.message
                : "Davet kabul edilirken beklenmeyen bir hata oluştu.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <main className="login-page">
            <section className="login-card" aria-labelledby="accept-title">
                <div className="login-brand">
                    <span className="login-brand-mark" aria-hidden="true">MRS</span>
                    <h1 id="accept-title">Hesabınızı Etkinleştirin</h1>
                    <p>Davetinizi tamamlamak için güvenli bir parola belirleyin.</p>
                </div>

                {!token && (
                    <p className="error-state" role="alert">
                        Davet bağlantısı geçersiz veya eksik. Yeni bir davet
                        bağlantısı isteyin.
                    </p>
                )}

                {isComplete && (
                    <div className="auth-result" role="status">
                        <p className="success-state">
                            Hesabınız hazır. Artık giriş yapabilirsiniz.
                        </p>
                        <Link className="button button-primary" to="/login">
                            Girişe Git
                        </Link>
                    </div>
                )}

                {token && !isComplete && (
                    <form className="login-form" onSubmit={handleSubmit}>
                        <div className="form-group">
                            <label htmlFor="invitation-password">Yeni Parola</label>
                            <input
                                id="invitation-password"
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
                            <label htmlFor="invitation-password-confirmation">
                                Yeni Parola Tekrar
                            </label>
                            <input
                                id="invitation-password-confirmation"
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
                            {isSubmitting ? "Hesap hazırlanıyor..." : "Hesabı Etkinleştir"}
                        </button>
                    </form>
                )}
            </section>
        </main>
    );
}
