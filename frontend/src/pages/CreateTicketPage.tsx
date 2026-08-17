import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";
import { Link, useNavigate } from "react-router-dom";

import { getAssets } from "../api/assetsApi";
import { getCategories } from "../api/categoriesApi";
import { ApiError } from "../api/httpClient";
import { createTicket } from "../api/ticketsApi";
import { useAuth } from "../auth/useAuth";

import type { AssetDto } from "../types/assets";
import type { TicketCategoryDto } from "../types/categories";
import type { TicketPriorityValue } from "../types/tickets";

const maxTitleLength = 200;
const maxDescriptionLength = 4000;

export function CreateTicketPage() {
    const navigate = useNavigate();
    const { token } = useAuth();

    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [categories, setCategories] = useState<TicketCategoryDto[]>([]);
    const [assetId, setAssetId] = useState("");
    const [categoryId, setCategoryId] = useState("");
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");
    const [priority, setPriority] = useState<TicketPriorityValue>(2);
    const [isAssetsLoading, setIsAssetsLoading] = useState(true);
    const [assetError, setAssetError] = useState<string | null>(null);
    const [isCategoriesLoading, setIsCategoriesLoading] = useState(true);
    const [categoryError, setCategoryError] = useState<string | null>(null);
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

    useEffect(() => {
        let cancelled = false;

        async function loadCategories() {
            if (!token) {
                if (!cancelled) {
                    setIsCategoriesLoading(false);
                }

                return;
            }

            try {
                setIsCategoriesLoading(true);
                setCategoryError(null);

                const result = await getCategories(token);

                if (!cancelled) {
                    setCategories(result);
                }
            } catch (error) {
                if (!cancelled) {
                    setCategoryError(getErrorMessage(
                        error,
                        "Kategoriler yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsCategoriesLoading(false);
                }
            }
        }

        loadCategories();

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

        if (!categoryId) {
            setSubmitError("Bir kategori seçmelisiniz.");
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
                        categoryId,
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

            {isCategoriesLoading && (
                <p className="loading-state">Kategoriler yükleniyor...</p>
            )}

            {categoryError && (
                <p className="error-state" role="alert">{categoryError}</p>
            )}

            {!isAssetsLoading &&
                !assetError &&
                activeAssets.length === 0 && (
                    <p className="empty-state">Aktif cihaz bulunamadı.</p>
                )}

            {!isCategoriesLoading &&
                !categoryError &&
                categories.length === 0 && (
                    <p className="empty-state">
                        Aktif kategori bulunamadı. Talep oluşturulamaz.
                    </p>
                )}

            {!isAssetsLoading && !assetError && (
                <form
                    className="card form-card"
                    onSubmit={handleSubmit}
                >
                    <div className="card-header">
                        <div>
                            <h2>Talep Bilgileri</h2>
                            <p className="page-description">
                                Cihazı ve kategoriyi seçin, ihtiyacı açık ve anlaşılır biçimde açıklayın.
                            </p>
                        </div>
                    </div>
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
                        <label htmlFor="category-id">Kategori</label>

                        <select
                            id="category-id"
                            value={categoryId}
                            onChange={(event) =>
                                setCategoryId(event.target.value)
                            }
                            disabled={
                                isSubmitting ||
                                isCategoriesLoading ||
                                Boolean(categoryError) ||
                                categories.length === 0
                            }
                            required
                        >
                            <option value="">Kategori seçin</option>
                            {categories.map((category) => (
                                <option
                                    key={category.id}
                                    value={category.id}
                                >
                                    {category.name}
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
                            <option value={1}>Düşük</option>
                            <option value={2}>Orta</option>
                            <option value={3}>Yüksek</option>
                            <option value={4}>Kritik</option>
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
                                activeAssets.length === 0 ||
                                isCategoriesLoading ||
                                Boolean(categoryError) ||
                                categories.length === 0
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

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
