import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import { getAssets } from "../api/assetsApi";
import { ApiError } from "../api/httpClient";
import { getTickets } from "../api/ticketsApi";
import { useAuth } from "../auth/useAuth";
import { AppNavigation } from "../components/AppNavigation";

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
    const { user, token, logout } = useAuth();

    const [result, setResult] = useState(emptyResult);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [status, setStatus] = useState<TicketStatusValue | "">("");
    const [priority, setPriority] =
        useState<TicketPriorityValue | "">("");
    const [assetId, setAssetId] = useState("");
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
        token,
    ]);

    function resetPage() {
        setPageNumber(1);
    }

    return (
        <main>
            <AppNavigation />

            <h1>Ticketlar</h1>

            <p>
                Hoş geldin, <strong>{user?.fullName}</strong>
            </p>

            <p>Rol: {user?.role}</p>

            <button type="button" onClick={logout}>
                Çıkış Yap
            </button>

            <hr />

            <h2>Talep Listesi</h2>

            <p>
                <Link to="/tickets/new">
                    Yeni Talep Oluştur
                </Link>
            </p>

            <section aria-label="Ticket filtreleri">
                <div>
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

                <div>
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

                <div>
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

                <div>
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

                <div>
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

                <div>
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
            </section>

            {assetError && <p role="alert">{assetError}</p>}
            {isLoading && <p>Ticketlar yükleniyor...</p>}
            {error && <p role="alert">{error}</p>}

            {!isLoading && !error && result.items.length === 0 && (
                <p>Gösterilecek ticket bulunamadı.</p>
            )}

            {!isLoading && !error && result.items.length > 0 && (
                <>
                    <table>
                        <thead>
                            <tr>
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
                                        <Link to={`/tickets/${ticket.id}`}>
                                            {ticket.title}
                                        </Link>
                                    </td>
                                    <td>{ticket.status}</td>
                                    <td>{ticket.priority}</td>
                                    <td>{ticket.assetName}</td>
                                    <td>{ticket.createdByFullName}</td>
                                    <td>
                                        {ticket.assignedTechnicianFullName ??
                                            "Atanmadı"}
                                    </td>
                                    <td>
                                        {new Date(ticket.createdAt)
                                            .toLocaleString("tr-TR")}
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
                        Önceki
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
                        Sonraki
                    </button>
                </>
            )}
        </main>
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
