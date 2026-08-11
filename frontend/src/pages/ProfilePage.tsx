import { useEffect, useState, type SubmitEvent } from "react";
import { useNavigate } from "react-router-dom";

import { changePassword, getCurrentUser } from "../api/authApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import { AccountStatusBadge, UserRoleBadge } from "../components/ManagementBadges";
import { validateNewPassword } from "../features/auth/passwordValidation";

import type { CurrentUser } from "../types/auth";

export function ProfilePage() {
    const { token, logout } = useAuth();
    const navigate = useNavigate();
    const [profile, setProfile] = useState<CurrentUser | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [pageError, setPageError] = useState<string | null>(null);
    const [currentPassword, setCurrentPassword] = useState("");
    const [newPassword, setNewPassword] = useState("");
    const [confirmation, setConfirmation] = useState("");
    const [formError, setFormError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);

    useEffect(() => {
        let cancelled = false;

        async function loadProfile() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                const currentUser = await getCurrentUser(token);

                if (!cancelled) {
                    setProfile(currentUser);
                    setPageError(null);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(error instanceof ApiError
                        ? error.message
                        : "Profil bilgileri yüklenemedi.");
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadProfile();

        return () => {
            cancelled = true;
        };
    }, [token]);

    async function handleChangePassword(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        if (!token || isSubmitting) {
            return;
        }

        if (!currentPassword) {
            setFormError("Mevcut parola zorunludur.");
            return;
        }

        const validationError = validateNewPassword(newPassword, confirmation);

        if (validationError) {
            setFormError(validationError);
            return;
        }

        try {
            setFormError(null);
            setIsSubmitting(true);
            await changePassword(token, { currentPassword, newPassword });
            setCurrentPassword("");
            setNewPassword("");
            setConfirmation("");
            logout();
            navigate("/login", {
                replace: true,
                state: {
                    message: "Şifreniz değiştirildi. Lütfen yeniden giriş yapın.",
                },
            });
        } catch (error) {
            setFormError(error instanceof ApiError
                ? error.message
                : "Şifre değiştirilirken beklenmeyen bir hata oluştu.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Profil</h1>
                    <p className="page-description">
                        Hesap bilgilerinizi görüntüleyin ve parolanızı değiştirin.
                    </p>
                </div>
            </header>

            {isLoading && <p className="loading-state">Profil yükleniyor...</p>}
            {pageError && <p className="error-state" role="alert">{pageError}</p>}

            {!isLoading && profile && (
                <section className="card" aria-labelledby="profile-details-title">
                    <h2 id="profile-details-title">Hesap Bilgileri</h2>
                    <dl className="definition-grid">
                        <div className="definition-item">
                            <dt>Ad Soyad</dt>
                            <dd>{profile.fullName}</dd>
                        </div>
                        <div className="definition-item">
                            <dt>E-posta</dt>
                            <dd>{profile.email}</dd>
                        </div>
                        <div className="definition-item">
                            <dt>Rol</dt>
                            <dd><UserRoleBadge role={profile.role} /></dd>
                        </div>
                        <div className="definition-item">
                            <dt>Departman</dt>
                            <dd>{profile.departmentName}</dd>
                        </div>
                        <div className="definition-item">
                            <dt>Hesap Durumu</dt>
                            <dd><AccountStatusBadge status={profile.accountStatus} /></dd>
                        </div>
                    </dl>
                </section>
            )}

            <section className="card form-card" aria-labelledby="change-password-title">
                <h2 id="change-password-title">Şifre Değiştir</h2>
                <form onSubmit={handleChangePassword}>
                    <div className="form-grid">
                        <div className="form-group form-group-full">
                            <label htmlFor="current-password">Mevcut Parola</label>
                            <input
                                id="current-password"
                                type="password"
                                autoComplete="current-password"
                                value={currentPassword}
                                onChange={(event) => setCurrentPassword(event.target.value)}
                                disabled={isSubmitting}
                                required
                            />
                        </div>
                        <div className="form-group">
                            <label htmlFor="profile-new-password">Yeni Parola</label>
                            <input
                                id="profile-new-password"
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
                            <label htmlFor="profile-new-password-confirmation">
                                Yeni Parola Tekrar
                            </label>
                            <input
                                id="profile-new-password-confirmation"
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
                    </div>

                    {formError && <p className="error-state" role="alert">{formError}</p>}

                    <div className="form-actions">
                        <button
                            type="submit"
                            className="button button-primary"
                            disabled={isSubmitting}
                        >
                            {isSubmitting ? "Değiştiriliyor..." : "Şifreyi Değiştir"}
                        </button>
                    </div>
                </form>
            </section>
        </div>
    );
}
