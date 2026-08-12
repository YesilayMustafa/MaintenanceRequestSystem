import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import { getAssets } from "../api/assetsApi";
import { ApiError } from "../api/httpClient";
import { getTickets } from "../api/ticketsApi";
import { useAuth } from "../auth/useAuth";
import {
    TicketPriorityBadge,
    TicketStatusBadge,
} from "../components/TicketBadges";

import type { AssetDto } from "../types/assets";
import type { PagedResult } from "../types/pagination";
import type {
    TicketDto,
    TicketPriorityValue,
    TicketSortBy,
    TicketStatusValue,
} from "../types/tickets";

const statusOptions: Array<{
    label: string;
    value: TicketStatusValue;
}> = [
    { label: "Open", value: 1 },
    { label: "Assigned", value: 2 },
    { label: "InProgress", value: 3 },
    { label: "Waiting", value: 4 },
    { label: "Resolved", value: 5 },
    { label: "Closed", value: 6 },
    { label: "Cancelled", value: 7 },
];

const priorityOptions: Array<{
    label: string;
    value: TicketPriorityValue;
}> = [
    { label: "Low", value: 1 },
    { label: "Medium", value: 2 },
    { label: "High", value: 3 },
    { label: "Critical", value: 4 },
];

const sortOptions: Array<{
    label: string;
    value: TicketSortBy;
}> = [
    { label: "Oluşturulma tarihi", value: "createdAt" },
    { label: "Başlık", value: "title" },
    { label: "Öncelik", value: "priority" },
    { label: "Durum", value: "status" },
];

const emptyResult: PagedResult<TicketDto> = {
    items: [],
    pageNumber: 1,
    pageSize: 10,
    totalCount: 0,
    totalPages: 0,
};

export function TicketsPage() {
    const { token } = useAuth();

    const [result, setResult] = useState(emptyResult);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [status, setStatus] = useState<TicketStatusValue | "">("");
    const [priority, setPriority] =
        useState<TicketPriorityValue | "">("");
    const [assetId, setAssetId] = useState("");
    const [ticketNumber, setTicketNumber] = useState("");
    const [sortBy, setSortBy] =
        useState<TicketSortBy>("createdAt");
    const [sortDescending, setSortDescending] = useState(true);
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [assetError, setAssetError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadAssets() {
            if (!token) {
                return;
            }

            try {
                setAssetError(null);
                const assetResult = await getAssets(token);

                if (!cancelled) {
                    setAssets(assetResult);
                }
            } catch (error) {
                if (!cancelled) {
                    setAssetError(getErrorMessage(
                        error,
                        "Cihaz filtresi yüklenemedi."
                    ));
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

        async function loadTickets() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setError(null);

                const ticketResult = await getTickets(token, {
                    pageNumber,
                    pageSize,
                    status: status || undefined,
                    priority: priority || undefined,
                    assetId: assetId || undefined,
                    ticketNumber: ticketNumber || undefined,
                    sortBy,
                    sortDescending,
                });

                if (!cancelled) {
                    setResult(ticketResult);
                }
            } catch (error) {
                if (!cancelled) {
                    setError(getErrorMessage(
                        error,
                        "Ticketlar yüklenirken beklenmeyen bir hata oluştu."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadTickets();

        return () => {
            cancelled = true;
        };
    }, [
        assetId,
        pageNumber,
        pageSize,
        priority,
        sortBy,
        sortDescending,
        status,
        ticketNumber,
        token,
    ]);

    function resetPage() {
        setPageNumber(1);
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Talepler</h1>
                    <p className="page-description">
                        Bakım ve arıza taleplerini izleyin, filtreleyin ve
                        yaşam döngüsü boyunca yönetin.
                    </p>
                </div>
                <div className="page-header-actions">
                    <Link to="/tickets/new" className="button button-primary">
                        Yeni Talep
                    </Link>
                </div>
            </header>

            <section className="card" aria-labelledby="ticket-filters-title">
                <div className="card-header">
                    <div>
                        <h2 id="ticket-filters-title">Filtreler</h2>
                        <p className="page-description">
                            Listeyi desteklenen alanlara göre daraltın ve sıralayın.
                        </p>
                    </div>
                </div>

                <div className="toolbar-grid">
                    <div className="form-group">
                        <label htmlFor="ticket-number-filter">Talep No</label>
                        <input
                            id="ticket-number-filter"
                            type="text"
                            value={ticketNumber}
                            maxLength={15}
                            placeholder="REQ-2026-"
                            onChange={(event) => {
                                setTicketNumber(event.target.value);
                                resetPage();
                            }}
                        />
                    </div>

                    <div className="form-group">
                    <label htmlFor="ticket-status-filter">Durum</label>
                    <select
                        id="ticket-status-filter"
                        value={status}
                        onChange={(event) => {
                            setStatus(
                                event.target.value
                                    ? Number(event.target.value) as
                                        TicketStatusValue
                                    : ""
                            );
                            resetPage();
                        }}
                    >
                        <option value="">Tümü</option>
                        {statusOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>

                    <div className="form-group">
                    <label htmlFor="ticket-priority-filter">Öncelik</label>
                    <select
                        id="ticket-priority-filter"
                        value={priority}
                        onChange={(event) => {
                            setPriority(
                                event.target.value
                                    ? Number(event.target.value) as
                                        TicketPriorityValue
                                    : ""
                            );
                            resetPage();
                        }}
                    >
                        <option value="">Tümü</option>
                        {priorityOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>

                    <div className="form-group">
                    <label htmlFor="ticket-asset-filter">Cihaz</label>
                    <select
                        id="ticket-asset-filter"
                        value={assetId}
                        onChange={(event) => {
                            setAssetId(event.target.value);
                            resetPage();
                        }}
                    >
                        <option value="">Tüm cihazlar</option>
                        {assets.map((asset) => (
                            <option key={asset.id} value={asset.id}>
                                {asset.name} ({asset.serialNumber})
                            </option>
                        ))}
                    </select>
                </div>

                    <div className="form-group">
                    <label htmlFor="ticket-sort-by">Sıralama</label>
                    <select
                        id="ticket-sort-by"
                        value={sortBy}
                        onChange={(event) => {
                            setSortBy(event.target.value as TicketSortBy);
                            resetPage();
                        }}
                    >
                        {sortOptions.map((option) => (
                            <option key={option.value} value={option.value}>
                                {option.label}
                            </option>
                        ))}
                    </select>
                </div>

                    <div className="form-group">
                    <label htmlFor="ticket-sort-direction">Yön</label>
                    <select
                        id="ticket-sort-direction"
                        value={sortDescending ? "descending" : "ascending"}
                        onChange={(event) => {
                            setSortDescending(
                                event.target.value === "descending"
                            );
                            resetPage();
                        }}
                    >
                        <option value="ascending">Artan</option>
                        <option value="descending">Azalan</option>
                    </select>
                </div>

                    <div className="form-group">
                    <label htmlFor="ticket-page-size">Sayfa Boyutu</label>
                    <select
                        id="ticket-page-size"
                        value={pageSize}
                        onChange={(event) => {
                            setPageSize(Number(event.target.value));
                            resetPage();
                        }}
                    >
                        <option value={10}>10</option>
                        <option value={25}>25</option>
                        <option value={50}>50</option>
                    </select>
                    </div>
                </div>
            </section>

            {assetError && (
                <p className="error-state" role="alert">{assetError}</p>
            )}
            {isLoading && (
                <p className="loading-state">Ticketlar yükleniyor...</p>
            )}
            {error && (
                <p className="error-state" role="alert">{error}</p>
            )}

            {!isLoading && !error && result.items.length === 0 && (
                <p className="empty-state">Gösterilecek ticket bulunamadı.</p>
            )}

            {!isLoading && !error && result.items.length > 0 && (
                <div className="table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Talep No</th>
                                <th>Başlık</th>
                                <th>Durum</th>
                                <th>Öncelik</th>
                                <th>Cihaz</th>
                                <th>Oluşturan</th>
                                <th>Teknisyen</th>
                                <th>Oluşturulma</th>
                            </tr>
                        </thead>

                        <tbody>
                            {result.items.map((ticket) => (
                                <tr key={ticket.id}>
                                    <td>
                                        <Link
                                            to={`/tickets/${ticket.id}`}
                                            className="table-link ticket-number"
                                        >
                                            {ticket.ticketNumber}
                                        </Link>
                                    </td>
                                    <td>
                                        <Link
                                            to={`/tickets/${ticket.id}`}
                                            className="table-link"
                                        >
                                            {ticket.title}
                                        </Link>
                                    </td>
                                    <td>
                                        <TicketStatusBadge status={ticket.status} />
                                    </td>
                                    <td>
                                        <TicketPriorityBadge
                                            priority={ticket.priority}
                                        />
                                    </td>
                                    <td>{ticket.assetName}</td>
                                    <td>{ticket.createdByFullName}</td>
                                    <td>
                                        {ticket.assignedTechnicianFullName ?? (
                                            <span className="muted-text">
                                                Atanmadı
                                            </span>
                                        )}
                                    </td>
                                    <td>
                                        {new Date(ticket.createdAt)
                                            .toLocaleString("tr-TR")}
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
                                Önceki
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
                                Sonraki
                            </button>
                        </div>
                    </div>
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
