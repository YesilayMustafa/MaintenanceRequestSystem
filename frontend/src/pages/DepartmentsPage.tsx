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
        <div className="page admin-page">

            <h1>Departmanlar</h1>

            {user?.role === "Admin" && !isFormVisible && (
                <button type="button" onClick={startCreate}>
                    Yeni Departman
                </button>
            )}

            {user?.role === "Admin" && isFormVisible && (
                <section>
                    <h2>
                        {editingDepartment
                            ? "Departman Düzenle"
                            : "Yeni Departman"}
                    </h2>

                    <form onSubmit={handleSubmit}>
                        <div>
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

                        <div>
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

                        <button
                            type="submit"
                            disabled={isSubmitting}
                        >
                            {isSubmitting
                                ? "Kaydediliyor..."
                                : "Kaydet"}
                        </button>

                        <button
                            type="button"
                            onClick={closeForm}
                            disabled={isSubmitting}
                        >
                            Vazgeç
                        </button>
                    </form>
                </section>
            )}

            {actionError && (
                <p role="alert">{actionError}</p>
            )}

            {isLoading && <p>Departmanlar yükleniyor...</p>}

            {pageError && <p role="alert">{pageError}</p>}

            {!isLoading && !pageError && departments.length === 0 && (
                <p>Departman bulunamadı.</p>
            )}

            {!isLoading && !pageError && departments.length > 0 && (
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
                                <td>{department.description ?? "-"}</td>
                                <td>
                                    {department.isActive ? "Aktif" : "Pasif"}
                                </td>

                                {user?.role === "Admin" && (
                                    <td>
                                        <button
                                            type="button"
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
                                    </td>
                                )}
                            </tr>
                        ))}
                    </tbody>
                </table>
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
