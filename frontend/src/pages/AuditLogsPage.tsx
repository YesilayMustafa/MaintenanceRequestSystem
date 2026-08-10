import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";

import { getAuditLogs } from "../api/auditLogsApi";
import { ApiError } from "../api/httpClient";
import { getUsers } from "../api/usersApi";
import { useAuth } from "../auth/useAuth";

import type { AuditLogDto } from "../types/audit";
import type { PagedResult } from "../types/pagination";
import type { UserDto } from "../types/users";

interface AuditFilters {
    performedByUserId: string;
    action: string;
    entityName: string;
    entityId: string;
    startDate: string;
    endDate: string;
}

const emptyFilters: AuditFilters = {
    performedByUserId: "",
    action: "",
    entityName: "",
    entityId: "",
    startDate: "",
    endDate: "",
};

const emptyResult: PagedResult<AuditLogDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
};

export function AuditLogsPage() {
    const { token } = useAuth();

    const [result, setResult] = useState(emptyResult);
    const [users, setUsers] = useState<UserDto[]>([]);
    const [draftFilters, setDraftFilters] = useState(emptyFilters);
    const [filters, setFilters] = useState(emptyFilters);
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [isLoading, setIsLoading] = useState(true);
    const [pageError, setPageError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadUsers() {
            if (!token) {
                return;
            }

            try {
                const userResult = await getUsers(token);

                if (!cancelled) {
                    setUsers(userResult);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(getErrorMessage(
                        error,
                        "Kullanıcı filtresi yüklenemedi."
                    ));
                }
            }
        }

        loadUsers();

        return () => {
            cancelled = true;
        };
    }, [token]);

    useEffect(() => {
        let cancelled = false;

        async function loadAuditLogs() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setPageError(null);

                const auditResult = await getAuditLogs(token, {
                    pageNumber,
                    pageSize,
                    performedByUserId:
                        filters.performedByUserId || undefined,
                    action: filters.action.trim() || undefined,
                    entityName: filters.entityName.trim() || undefined,
                    entityId: filters.entityId.trim() || undefined,
                    startDate: filters.startDate
                        ? toUtcStart(filters.startDate)
                        : undefined,
                    endDate: filters.endDate
                        ? toUtcEnd(filters.endDate)
                        : undefined,
                });

                if (!cancelled) {
                    setResult(auditResult);
                }
            } catch (error) {
                if (!cancelled) {
                    setPageError(getErrorMessage(
                        error,
                        "Audit logları yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadAuditLogs();

        return () => {
            cancelled = true;
        };
    }, [filters, pageNumber, pageSize, token]);

    function handleFilterSubmit(
        event: SubmitEvent<HTMLFormElement>
    ) {
        event.preventDefault();
        setPageNumber(1);
        setFilters(draftFilters);
    }

    function clearFilters() {
        setDraftFilters(emptyFilters);
        setFilters(emptyFilters);
        setPageNumber(1);
    }

    return (
        <div className="page admin-page">

            <h1>Audit Logları</h1>

            <form onSubmit={handleFilterSubmit}>
                <div>
                    <label htmlFor="audit-action">Action</label>
                    <input
                        id="audit-action"
                        value={draftFilters.action}
                        onChange={(event) =>
                            setDraftFilters({
                                ...draftFilters,
                                action: event.target.value,
                            })
                        }
                    />
                </div>

                <div>
                    <label htmlFor="audit-entity-name">Entity</label>
                    <input
                        id="audit-entity-name"
                        value={draftFilters.entityName}
                        onChange={(event) =>
                            setDraftFilters({
                                ...draftFilters,
                                entityName: event.target.value,
                            })
                        }
                    />
                </div>

                <div>
                    <label htmlFor="audit-user">Kullanıcı</label>
                    <select
                        id="audit-user"
                        value={draftFilters.performedByUserId}
                        onChange={(event) =>
                            setDraftFilters({
                                ...draftFilters,
                                performedByUserId: event.target.value,
                            })
                        }
                    >
                        <option value="">Tümü</option>
                        {users.map((user) => (
                            <option key={user.id} value={user.id}>
                                {user.fullName}
                            </option>
                        ))}
                    </select>
                </div>

                <div>
                    <label htmlFor="audit-entity-id">Entity ID</label>
                    <input
                        id="audit-entity-id"
                        value={draftFilters.entityId}
                        onChange={(event) =>
                            setDraftFilters({
                                ...draftFilters,
                                entityId: event.target.value,
                            })
                        }
                    />
                </div>

                <div>
                    <label htmlFor="audit-start-date">Başlangıç</label>
                    <input
                        id="audit-start-date"
                        type="date"
                        value={draftFilters.startDate}
                        onChange={(event) =>
                            setDraftFilters({
                                ...draftFilters,
                                startDate: event.target.value,
                            })
                        }
                    />
                </div>

                <div>
                    <label htmlFor="audit-end-date">Bitiş</label>
                    <input
                        id="audit-end-date"
                        type="date"
                        value={draftFilters.endDate}
                        onChange={(event) =>
                            setDraftFilters({
                                ...draftFilters,
                                endDate: event.target.value,
                            })
                        }
                    />
                </div>

                <button type="submit" disabled={isLoading}>
                    Filtrele
                </button>
                <button
                    type="button"
                    onClick={clearFilters}
                    disabled={isLoading}
                >
                    Temizle
                </button>
            </form>

            <div>
                <label htmlFor="audit-page-size">Sayfa Boyutu</label>
                <select
                    id="audit-page-size"
                    value={pageSize}
                    onChange={(event) => {
                        setPageSize(Number(event.target.value));
                        setPageNumber(1);
                    }}
                    disabled={isLoading}
                >
                    <option value={10}>10</option>
                    <option value={25}>25</option>
                    <option value={50}>50</option>
                </select>
            </div>

            {isLoading && <p>Audit logları yükleniyor...</p>}
            {pageError && <p role="alert">{pageError}</p>}

            {!isLoading && !pageError && result.items.length === 0 && (
                <p>Audit kaydı bulunamadı.</p>
            )}

            {!isLoading && !pageError && result.items.length > 0 && (
                <>
                    <table>
                        <thead>
                            <tr>
                                <th>Tarih</th>
                                <th>Kullanıcı</th>
                                <th>Action</th>
                                <th>Entity</th>
                                <th>Entity ID</th>
                                <th>Eski Değerler</th>
                                <th>Yeni Değerler</th>
                            </tr>
                        </thead>

                        <tbody>
                            {result.items.map((auditLog) => (
                                <tr key={auditLog.id}>
                                    <td>
                                        {new Date(auditLog.createdAt)
                                            .toLocaleString("tr-TR")}
                                    </td>
                                    <td>
                                        {auditLog.performedByUserFullName}
                                    </td>
                                    <td>{auditLog.action}</td>
                                    <td>{auditLog.entityName}</td>
                                    <td>{auditLog.entityId}</td>
                                    <td>
                                        <pre>{auditLog.oldValues ?? "-"}</pre>
                                    </td>
                                    <td>
                                        <pre>{auditLog.newValues ?? "-"}</pre>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    <p>
                        Sayfa {result.pageNumber} / {result.totalPages} —
                        Toplam {result.totalCount} kayıt
                    </p>

                    <button
                        type="button"
                        onClick={() => setPageNumber(
                            (currentPage) => currentPage - 1
                        )}
                        disabled={isLoading || pageNumber <= 1}
                    >
                        Önceki Sayfa
                    </button>

                    <button
                        type="button"
                        onClick={() => setPageNumber(
                            (currentPage) => currentPage + 1
                        )}
                        disabled={
                            isLoading ||
                            result.totalPages === 0 ||
                            pageNumber >= result.totalPages
                        }
                    >
                        Sonraki Sayfa
                    </button>
                </>
            )}
        </div>
    );
}

function toUtcStart(date: string): string {
    return `${date}T00:00:00.000Z`;
}

function toUtcEnd(date: string): string {
    return `${date}T23:59:59.999Z`;
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
