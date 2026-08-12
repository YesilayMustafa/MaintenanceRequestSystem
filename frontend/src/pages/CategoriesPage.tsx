import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";

import {
    changeCategoryStatus,
    createCategory,
    getCategories,
    updateCategory,
} from "../api/categoriesApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import { ActiveStatusBadge } from "../components/ManagementBadges";

import type { TicketCategoryDto } from "../types/categories";

const maxNameLength = 100;
const maxDescriptionLength = 500;

export function CategoriesPage() {
    const { token } = useAuth();

    const [categories, setCategories] = useState<TicketCategoryDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [pageError, setPageError] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [actionSuccess, setActionSuccess] = useState<string | null>(null);
    const [isFormVisible, setIsFormVisible] = useState(false);
    const [editingCategory, setEditingCategory] =
        useState<TicketCategoryDto | null>(null);
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [statusCategoryId, setStatusCategoryId] =
        useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadCategories() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setPageError(null);

                const result = await getCategories(token, true);

                if (!cancelled) {
                    setCategories(result);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(getErrorMessage(
                        error,
                        "Kategoriler yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadCategories();

        return () => {
            cancelled = true;
        };
    }, [token]);

    async function refreshCategories() {
        if (!token) {
            return;
        }

        setCategories(await getCategories(token, true));
    }

    function resetForm() {
        setEditingCategory(null);
        setName("");
        setDescription("");
    }

    function startCreate() {
        resetForm();
        setActionError(null);
        setActionSuccess(null);
        setIsFormVisible(true);
    }

    function startEdit(category: TicketCategoryDto) {
        setEditingCategory(category);
        setName(category.name);
        setDescription(category.description ?? "");
        setActionError(null);
        setActionSuccess(null);
        setIsFormVisible(true);
    }

    function closeForm() {
        setIsFormVisible(false);
        resetForm();
    }

    async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        if (!token || isSubmitting) {
            return;
        }

        const normalizedName = name.trim();
        const normalizedDescription = description.trim();

        if (!normalizedName) {
            setActionError("Kategori adı boş olamaz.");
            return;
        }

        try {
            setIsSubmitting(true);
            setActionError(null);
            setActionSuccess(null);

            const request = {
                name: normalizedName,
                description: normalizedDescription || null,
            };

            if (editingCategory) {
                await updateCategory(token, editingCategory.id, request);
                setActionSuccess("Kategori güncellendi.");
            } else {
                await createCategory(token, request);
                setActionSuccess("Kategori oluşturuldu.");
            }

            await refreshCategories();
            closeForm();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Kategori kaydedilemedi."
            ));
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleStatusChange(category: TicketCategoryDto) {
        if (!token || statusCategoryId) {
            return;
        }

        try {
            setStatusCategoryId(category.id);
            setActionError(null);
            setActionSuccess(null);

            await changeCategoryStatus(
                token,
                category.id,
                { isActive: !category.isActive }
            );

            await refreshCategories();
            setActionSuccess(
                category.isActive
                    ? "Kategori pasifleştirildi."
                    : "Kategori aktifleştirildi."
            );
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Kategori durumu değiştirilemedi."
            ));
        } finally {
            setStatusCategoryId(null);
        }
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Kategoriler</h1>
                    <p className="page-description">
                        Taleplerde kullanılan kategori seçeneklerini yönetin.
                    </p>
                </div>

                {!isFormVisible && (
                    <button
                        type="button"
                        className="button button-primary"
                        onClick={startCreate}
                    >
                        Yeni Kategori
                    </button>
                )}
            </header>

            {isFormVisible && (
                <section className="card management-form-card">
                    <div className="card-header">
                        <div>
                            <h2>
                                {editingCategory
                                    ? "Kategoriyi Düzenle"
                                    : "Yeni Kategori"}
                            </h2>
                            <p className="page-description">
                                Kategori adı ve isteğe bağlı açıklamayı girin.
                            </p>
                        </div>
                    </div>

                    <form onSubmit={handleSubmit}>
                        <div className="form-grid">
                            <div className="form-group">
                                <label htmlFor="category-name">Ad</label>
                                <input
                                    id="category-name"
                                    value={name}
                                    maxLength={maxNameLength}
                                    onChange={(event) =>
                                        setName(event.target.value)
                                    }
                                    disabled={isSubmitting}
                                />
                            </div>

                            <div className="form-group form-group-full">
                                <label htmlFor="category-description">
                                    Açıklama
                                </label>
                                <textarea
                                    id="category-description"
                                    value={description}
                                    maxLength={maxDescriptionLength}
                                    onChange={(event) =>
                                        setDescription(event.target.value)
                                    }
                                    disabled={isSubmitting}
                                />
                            </div>
                        </div>

                        <div className="form-actions">
                            <button
                                type="submit"
                                className="button button-primary"
                                disabled={isSubmitting}
                            >
                                {isSubmitting ? "Kaydediliyor..." : "Kaydet"}
                            </button>
                            <button
                                type="button"
                                className="button button-secondary"
                                onClick={closeForm}
                                disabled={isSubmitting}
                            >
                                Vazgeç
                            </button>
                        </div>
                    </form>
                </section>
            )}

            {actionError && (
                <p className="error-state" role="alert">{actionError}</p>
            )}
            {actionSuccess && (
                <p className="success-state" role="status">{actionSuccess}</p>
            )}
            {isLoading && (
                <p className="loading-state">Kategoriler yükleniyor...</p>
            )}
            {pageError && (
                <p className="error-state" role="alert">{pageError}</p>
            )}

            {!isLoading && !pageError && categories.length === 0 && (
                <p className="empty-state">Kategori bulunamadı.</p>
            )}

            {!isLoading && !pageError && categories.length > 0 && (
                <div className="table-container category-table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Ad</th>
                                <th>Açıklama</th>
                                <th>Durum</th>
                                <th>Oluşturulma</th>
                                <th>Son Güncelleme</th>
                                <th>İşlemler</th>
                            </tr>
                        </thead>
                        <tbody>
                            {categories.map((category) => (
                                <tr key={category.id}>
                                    <td>{category.name}</td>
                                    <td>
                                        {category.description ?? (
                                            <span className="muted-text">
                                                Açıklama yok
                                            </span>
                                        )}
                                    </td>
                                    <td>
                                        <ActiveStatusBadge
                                            isActive={category.isActive}
                                        />
                                    </td>
                                    <td>{formatDate(category.createdAt)}</td>
                                    <td>
                                        {category.updatedAt
                                            ? formatDate(category.updatedAt)
                                            : (
                                                <span className="muted-text">
                                                    Güncellenmedi
                                                </span>
                                            )}
                                    </td>
                                    <td>
                                        <div className="action-buttons">
                                            <button
                                                type="button"
                                                className="button button-secondary button-small"
                                                onClick={() => startEdit(category)}
                                                disabled={
                                                    isSubmitting ||
                                                    statusCategoryId !== null
                                                }
                                            >
                                                Düzenle
                                            </button>
                                            <button
                                                type="button"
                                                className="button button-secondary button-small"
                                                onClick={() =>
                                                    handleStatusChange(category)
                                                }
                                                disabled={
                                                    isSubmitting ||
                                                    statusCategoryId !== null
                                                }
                                            >
                                                {statusCategoryId === category.id
                                                    ? "Değiştiriliyor..."
                                                    : category.isActive
                                                        ? "Pasif Yap"
                                                        : "Aktif Yap"}
                                            </button>
                                        </div>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </div>
    );
}

function formatDate(value: string): string {
    return new Date(value).toLocaleString("tr-TR");
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
