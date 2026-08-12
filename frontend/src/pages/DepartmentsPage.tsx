import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";

import {
    changeDepartmentStatus,
    createDepartment,
    getDepartments,
    updateDepartment,
} from "../api/departmentsApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import { ActiveStatusBadge } from "../components/ManagementBadges";

import type { DepartmentDto } from "../types/departments";

export function DepartmentsPage() {
    const { user, token } = useAuth();

    const [departments, setDepartments] = useState<DepartmentDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [pageError, setPageError] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [isFormVisible, setIsFormVisible] = useState(false);
    const [editingDepartment, setEditingDepartment] =
        useState<DepartmentDto | null>(null);
    const [name, setName] = useState("");
    const [description, setDescription] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [statusDepartmentId, setStatusDepartmentId] =
        useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadDepartments() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setPageError(null);

                const result = await getDepartments(token);

                if (!cancelled) {
                    setDepartments(result);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(getErrorMessage(
                        error,
                        "Departmanlar yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadDepartments();

        return () => {
            cancelled = true;
        };
    }, [token]);

    async function refreshDepartments() {
        if (!token) {
            return;
        }

        const result = await getDepartments(token);
        setDepartments(result);
    }

    function startCreate() {
        setEditingDepartment(null);
        setName("");
        setDescription("");
        setActionError(null);
        setIsFormVisible(true);
    }

    function startEdit(department: DepartmentDto) {
        setEditingDepartment(department);
        setName(department.name);
        setDescription(department.description ?? "");
        setActionError(null);
        setIsFormVisible(true);
    }

    function closeForm() {
        setIsFormVisible(false);
        setEditingDepartment(null);
        setName("");
        setDescription("");
    }

    async function handleSubmit(
        event: SubmitEvent<HTMLFormElement>
    ) {
        event.preventDefault();

        if (!token || isSubmitting) {
            return;
        }

        const normalizedName = name.trim();
        const normalizedDescription = description.trim();

        if (!normalizedName) {
            setActionError("Departman adı boş olamaz.");
            return;
        }

        try {
            setIsSubmitting(true);
            setActionError(null);

            const request = {
                name: normalizedName,
                description: normalizedDescription || null,
            };

            if (editingDepartment) {
                await updateDepartment(
                    token,
                    editingDepartment.id,
                    request
                );
            } else {
                await createDepartment(token, request);
            }

            await refreshDepartments();
            closeForm();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Departman kaydedilemedi."
            ));
        } finally {
            setIsSubmitting(false);
        }
    }

    async function handleStatusChange(
        department: DepartmentDto
    ) {
        if (!token || statusDepartmentId) {
            return;
        }

        try {
            setStatusDepartmentId(department.id);
            setActionError(null);

            await changeDepartmentStatus(
                token,
                department.id,
                { isActive: !department.isActive }
            );

            await refreshDepartments();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Departman durumu değiştirilemedi."
            ));
        } finally {
            setStatusDepartmentId(null);
        }
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Departmanlar</h1>
                    <p className="page-description">
                        Kullanıcıları ve cihazları organizasyon birimlerine göre
                        düzenleyin.
                    </p>
                </div>

            {user?.role === "Admin" && !isFormVisible && (
                    <button
                        type="button"
                        className="button button-primary"
                        onClick={startCreate}
                    >
                    Yeni Departman
                </button>
            )}
            </header>

            {user?.role === "Admin" && isFormVisible && (
                <section className="card management-form-card">
                    <div className="card-header">
                        <div>
                            <h2>
                                {editingDepartment
                                    ? "Departmanı Düzenle"
                                    : "Yeni Departman"}
                            </h2>
                            <p className="page-description">
                                {editingDepartment
                                    ? "Departman adını ve açıklamasını güncelleyin."
                                    : "Organizasyona yeni bir departman ekleyin."}
                            </p>
                        </div>
                    </div>

                    <form onSubmit={handleSubmit}>
                        <div className="form-grid">
                        <div className="form-group">
                            <label htmlFor="department-name">Ad</label>

                            <input
                                id="department-name"
                                value={name}
                                onChange={(event) =>
                                    setName(event.target.value)
                                }
                                disabled={isSubmitting}
                            />
                        </div>

                        <div className="form-group form-group-full">
                            <label htmlFor="department-description">
                                Açıklama
                            </label>

                            <textarea
                                id="department-description"
                                value={description}
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
                                {isSubmitting
                                    ? "Kaydediliyor..."
                                    : "Kaydet"}
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

            {isLoading && (
                <p className="loading-state">Departmanlar yükleniyor...</p>
            )}

            {pageError && (
                <p className="error-state" role="alert">{pageError}</p>
            )}

            {!isLoading && !pageError && departments.length === 0 && (
                <p className="empty-state">Departman bulunamadı.</p>
            )}

            {!isLoading && !pageError && departments.length > 0 && (
                <div className="table-container">
                <table>
                    <thead>
                        <tr>
                            <th>Ad</th>
                            <th>Açıklama</th>
                            <th>Durum</th>
                            {user?.role === "Admin" && <th>İşlemler</th>}
                        </tr>
                    </thead>

                    <tbody>
                        {departments.map((department) => (
                            <tr key={department.id}>
                                <td>{department.name}</td>
                                <td>
                                    {department.description ?? (
                                        <span className="muted-text">
                                            Açıklama yok
                                        </span>
                                    )}
                                </td>
                                <td>
                                    <ActiveStatusBadge
                                        isActive={department.isActive}
                                    />
                                </td>

                                {user?.role === "Admin" && (
                                    <td>
                                        <div className="action-buttons">
                                        <button
                                            type="button"
                                            className="button button-secondary button-small"
                                            onClick={() => startEdit(department)}
                                            disabled={
                                                isSubmitting ||
                                                statusDepartmentId !== null
                                            }
                                        >
                                            Düzenle
                                        </button>

                                        <button
                                            type="button"
                                            className="button button-secondary button-small"
                                            onClick={() =>
                                                handleStatusChange(department)
                                            }
                                            disabled={
                                                isSubmitting ||
                                                statusDepartmentId !== null
                                            }
                                        >
                                            {statusDepartmentId === department.id
                                                ? "Değiştiriliyor..."
                                                : department.isActive
                                                    ? "Pasif Yap"
                                                    : "Aktif Yap"}
                                        </button>
                                        </div>
                                    </td>
                                )}
                            </tr>
                        ))}
                    </tbody>
                </table>
                </div>
            )}
        </div>
    );
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
