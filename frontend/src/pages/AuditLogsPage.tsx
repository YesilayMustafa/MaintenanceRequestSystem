import {
    useEffect,
    useRef,
    useState,
    type ReactNode,
    type SubmitEvent,
} from "react";
import { Link } from "react-router-dom";

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
    const [selectedAuditLog, setSelectedAuditLog] =
        useState<AuditLogDto | null>(null);

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
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Audit Logları</h1>
                    <p className="page-description">
                        Sistemdeki önemli işlemleri ve veri değişikliklerini
                        zaman, kullanıcı ve entity bilgileriyle izleyin.
                    </p>
                </div>
            </header>

            <section className="card" aria-labelledby="audit-filters-title">
                <div className="card-header">
                    <div>
                        <h2 id="audit-filters-title">Filtreler</h2>
                        <p className="page-description">
                            Audit kayıtlarını desteklenen alanlara göre daraltın.
                        </p>
                    </div>
                </div>

                <form onSubmit={handleFilterSubmit}>
                    <div className="audit-toolbar-grid">
                <div className="form-group">
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

                <div className="form-group">
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

                <div className="form-group">
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

                <div className="form-group">
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

                <div className="form-group">
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

                <div className="form-group">
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

                <div className="form-group">
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
                    </div>

                    <div className="form-actions">
                        <button
                            type="submit"
                            className="button button-primary"
                            disabled={isLoading}
                        >
                            Filtrele
                        </button>
                        <button
                            type="button"
                            className="button button-secondary"
                            onClick={clearFilters}
                            disabled={isLoading}
                        >
                            Temizle
                        </button>
                    </div>
                </form>
            </section>

            {isLoading && (
                <p className="loading-state">Audit logları yükleniyor...</p>
            )}
            {pageError && (
                <p className="error-state" role="alert">{pageError}</p>
            )}

            {!isLoading && !pageError && result.items.length === 0 && (
                <p className="empty-state">Audit kaydı bulunamadı.</p>
            )}

            {!isLoading && !pageError && result.items.length > 0 && (
                <div className="table-container audit-table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Tarih</th>
                                <th>Kullanıcı</th>
                                <th>Action</th>
                                <th>Entity</th>
                                <th>Entity ID</th>
                                <th>Detay</th>
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
                                    <td className="identifier-cell">
                                        {auditLog.entityId}
                                    </td>
                                    <td>
                                        <button
                                            type="button"
                                            className="button-link audit-detail-trigger"
                                            onClick={(event) => {
                                                event.stopPropagation();
                                                setSelectedAuditLog(auditLog);
                                            }}
                                        >
                                            Detayı incele
                                        </button>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>

                    <div className="pagination">
                        <p className="pagination-summary">
                            Sayfa {result.pageNumber} / {result.totalPages} —
                            Toplam {result.totalCount} kayıt
                        </p>
                        <div className="pagination-actions">
                            <button
                                type="button"
                                className="button button-secondary button-small"
                                onClick={() => setPageNumber(
                                    (currentPage) => currentPage - 1
                                )}
                                disabled={isLoading || pageNumber <= 1}
                            >
                                Önceki Sayfa
                            </button>

                            <button
                                type="button"
                                className="button button-secondary button-small"
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
                        </div>
                    </div>
                </div>
            )}

            {selectedAuditLog && (
                <AuditDetailDrawer
                    auditLog={selectedAuditLog}
                    onClose={() => setSelectedAuditLog(null)}
                />
            )}
        </div>
    );
}

function AuditDetailDrawer({
    auditLog,
    onClose,
}: {
    auditLog: AuditLogDto;
    onClose: () => void;
}) {
    const closeButtonRef = useRef<HTMLButtonElement>(null);
    const [copyFeedback, setCopyFeedback] = useState(false);

    useEffect(() => {
        closeButtonRef.current?.focus();

        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === "Escape") {
                onClose();
            }
        }

        document.addEventListener("keydown", handleKeyDown);
        return () => document.removeEventListener("keydown", handleKeyDown);
    }, [onClose]);

    async function copyEntityId() {
        if (!navigator.clipboard) {
            return;
        }

        await navigator.clipboard.writeText(auditLog.entityId);
        setCopyFeedback(true);
        window.setTimeout(() => setCopyFeedback(false), 1600);
    }

    return (
        <div
            className="detail-drawer-backdrop"
            role="presentation"
            onMouseDown={(event) => {
                if (event.target === event.currentTarget) {
                    onClose();
                }
            }}
        >
            <aside
                className="detail-drawer"
                role="dialog"
                aria-modal="true"
                aria-labelledby="audit-detail-title"
            >
                <header className="detail-drawer-header">
                    <div>
                        <span className="eyebrow">Audit kaydı</span>
                        <h2 id="audit-detail-title">İşlem detayı</h2>
                    </div>
                    <button
                        ref={closeButtonRef}
                        type="button"
                        className="icon-button"
                        aria-label="Detayı kapat"
                        onClick={onClose}
                    >
                        ×
                    </button>
                </header>

                <dl className="audit-detail-list">
                    <DetailItem label="Tarih ve saat">
                        {new Date(auditLog.createdAt).toLocaleString("tr-TR")}
                    </DetailItem>
                    <DetailItem label="İşlemi yapan">
                        {auditLog.performedByUserFullName}
                    </DetailItem>
                    <DetailItem label="Action">{auditLog.action}</DetailItem>
                    <DetailItem label="Entity">{auditLog.entityName}</DetailItem>
                    <DetailItem label="Entity ID">
                        <div className="audit-identifier-row">
                            <code>{auditLog.entityId}</code>
                            <button
                                type="button"
                                className="button button-secondary button-small"
                                onClick={copyEntityId}
                            >
                                Kopyala
                            </button>
                            <span className="copy-feedback" role="status">
                                {copyFeedback ? "Kopyalandı" : ""}
                            </span>
                        </div>
                    </DetailItem>
                </dl>

                {auditLog.entityName === "Ticket" && (
                    <Link
                        className="button button-secondary audit-entity-link"
                        to={`/tickets/${auditLog.entityId}`}
                    >
                        Talebi Aç
                    </Link>
                )}

                <AuditValueBlock title="Eski Değerler" value={auditLog.oldValues} />
                <AuditValueBlock title="Yeni Değerler" value={auditLog.newValues} />
            </aside>
        </div>
    );
}

function DetailItem({ label, children }: { label: string; children: ReactNode }) {
    return (
        <div>
            <dt>{label}</dt>
            <dd>{children}</dd>
        </div>
    );
}

function AuditValueBlock({ title, value }: { title: string; value: string | null }) {
    return (
        <section className="audit-value-block">
            <h3>{title}</h3>
            <pre>{formatJson(value)}</pre>
        </section>
    );
}

function formatJson(value: string | null): string {
    if (!value) {
        return "-";
    }

    try {
        return JSON.stringify(JSON.parse(value), null, 2);
    } catch {
        return value;
    }
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
