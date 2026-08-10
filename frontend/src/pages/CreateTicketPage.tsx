import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";
import { Link, useNavigate } from "react-router-dom";

import { getAssets } from "../api/assetsApi";
import { ApiError } from "../api/httpClient";
import { createTicket } from "../api/ticketsApi";
import { useAuth } from "../auth/useAuth";

import type { AssetDto } from "../types/assets";
import type { TicketPriorityValue } from "../types/tickets";

const maxTitleLength = 200;
const maxDescriptionLength = 4000;

export function CreateTicketPage() {
    const navigate = useNavigate();
    const { token } = useAuth();

    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [assetId, setAssetId] = useState("");
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [priority, setPriority] = useState<TicketPriorityValue>(2);
    const [isAssetsLoading, setIsAssetsLoading] = useState(true);
    const [assetError, setAssetError] = useState<string | null>(null);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [submitError, setSubmitError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadAssets() {
            if (!token) {
                if (!cancelled) {
                    setIsAssetsLoading(false);
                }

                return;
            }

            try {
                setIsAssetsLoading(true);
                setAssetError(null);

                const result = await getAssets(token);

                if (!cancelled) {
                    setAssets(result);
                }
            } catch (error) {
                if (cancelled) {
                    return;
                }

                if (error instanceof ApiError) {
                    setAssetError(error.message);
                } else {
                    setAssetError(
                        "Cihazlar yüklenirken beklenmeyen bir hata oluştu."
                    );
                }
            } finally {
                if (!cancelled) {
                    setIsAssetsLoading(false);
                }
            }
        }

        loadAssets();

        return () => {
            cancelled = true;
        };
    }, [token]);

    const activeAssets =
        assets.filter((asset) => asset.isActive);

    async function handleSubmit(
        event: SubmitEvent<HTMLFormElement>
    ) {
        event.preventDefault();

        if (isSubmitting || !token) {
            return;
        }

        const normalizedTitle = title.trim();
        const normalizedDescription = description.trim();

        if (!assetId) {
            setSubmitError("Bir cihaz seçmelisiniz.");
            return;
        }

        if (!normalizedTitle) {
            setSubmitError("Başlık boş olamaz.");
            return;
        }

        if (!normalizedDescription) {
            setSubmitError("Açıklama boş olamaz.");
            return;
        }

        if (!isTicketPriorityValue(priority)) {
            setSubmitError("Geçerli bir öncelik seçmelisiniz.");
            return;
        }

        try {
            setIsSubmitting(true);
            setSubmitError(null);

            const createdTicket =
                await createTicket(
                    token,
                    {
                        assetId,
                        title: normalizedTitle,
                        description: normalizedDescription,
                        priority,
                    }
                );

            navigate(`/tickets/${createdTicket.id}`);
        } catch (error) {
            if (error instanceof ApiError) {
                setSubmitError(error.message);
            } else {
                setSubmitError(
                    "Ticket oluşturulurken beklenmeyen bir hata oluştu."
                );
            }
        } finally {
            setIsSubmitting(false);
        }
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Yeni Talep Oluştur</h1>
                    <p className="page-description">
                        Arıza veya bakım ihtiyacını ilgili cihazla birlikte kaydedin.
                    </p>
                </div>
                <div className="page-header-actions">
                    <Link to="/tickets" className="button button-secondary">
                        Talep Listesine Dön
                    </Link>
                </div>
            </header>

            {isAssetsLoading && (
                <p className="loading-state">Cihazlar yükleniyor...</p>
            )}

            {assetError && (
                <p className="error-state" role="alert">{assetError}</p>
            )}

            {!isAssetsLoading &&
                !assetError &&
                activeAssets.length === 0 && (
                    <p className="empty-state">Aktif cihaz bulunamadı.</p>
                )}

            {!isAssetsLoading && !assetError && (
                <form
                    className="card form-card"
                    onSubmit={handleSubmit}
                >
                    <div className="form-grid">
                    <div className="form-group form-group-full">
                        <label htmlFor="asset-id">Cihaz</label>

                        <select
                            id="asset-id"
                            value={assetId}
                            onChange={(event) =>
                                setAssetId(event.target.value)
                            }
                            disabled={
                                isSubmitting ||
                                activeAssets.length === 0
                            }
                        >
                            <option value="">Cihaz seçin</option>

                            {activeAssets.map((asset) => (
                                <option
                                    key={asset.id}
                                    value={asset.id}
                                >
                                    {asset.name}
                                    {" - "}
                                    {asset.serialNumber}
                                    {" - "}
                                    {asset.departmentName}
                                </option>
                            ))}
                        </select>
                    </div>

                    <div className="form-group form-group-full">
                        <label htmlFor="ticket-title">Başlık</label>

                        <input
                            id="ticket-title"
                            type="text"
                            value={title}
                            maxLength={maxTitleLength}
                            onChange={(event) =>
                                setTitle(event.target.value)
                            }
                            disabled={isSubmitting}
                        />
                    </div>

                    <div className="form-group form-group-full">
                        <label htmlFor="ticket-description">
                            Açıklama
                        </label>

                        <textarea
                            id="ticket-description"
                            value={description}
                            maxLength={maxDescriptionLength}
                            onChange={(event) =>
                                setDescription(event.target.value)
                            }
                            disabled={isSubmitting}
                        />
                    </div>

                    <div className="form-group">
                        <label htmlFor="ticket-priority">
                            Öncelik
                        </label>

                        <select
                            id="ticket-priority"
                            value={priority}
                            onChange={(event) =>
                                setPriority(
                                    Number(event.target.value) as
                                        TicketPriorityValue
                                )
                            }
                            disabled={isSubmitting}
                        >
                            <option value={1}>Low</option>
                            <option value={2}>Medium</option>
                            <option value={3}>High</option>
                            <option value={4}>Critical</option>
                        </select>
                    </div>

                    </div>

                    {submitError && (
                        <p className="error-state" role="alert">
                            {submitError}
                        </p>
                    )}

                    <div className="form-actions">
                        <button
                            type="submit"
                            className="button button-primary"
                            disabled={
                                isSubmitting ||
                                activeAssets.length === 0
                            }
                        >
                            {isSubmitting
                                ? "Oluşturuluyor..."
                                : "Talebi Oluştur"}
                        </button>
                        <Link to="/tickets" className="button button-secondary">
                            Vazgeç
                        </Link>
                    </div>
                </form>
            )}
        </div>
    );
}

function isTicketPriorityValue(
    value: number
): value is TicketPriorityValue {
    return value === 1 ||
        value === 2 ||
        value === 3 ||
        value === 4;
}
