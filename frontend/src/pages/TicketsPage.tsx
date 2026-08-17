import {
    useEffect,
    useState,
    type FormEvent,
} from "react";
import { Link, useSearchParams } from "react-router-dom";

import { getAssets } from "../api/assetsApi";
import { getCategories } from "../api/categoriesApi";
import { getDepartments } from "../api/departmentsApi";
import { ApiError } from "../api/httpClient";
import { getTickets } from "../api/ticketsApi";
import { getUsers } from "../api/usersApi";
import { useAuth } from "../auth/useAuth";
import {
    TicketPriorityBadge,
    TicketStatusBadge,
} from "../components/TicketBadges";
import { SlaBadge } from "../components/SlaBadge";
import { formatSlaRemainingTime } from "../utils/sla";

import type { AssetDto } from "../types/assets";
import type { TicketCategoryDto } from "../types/categories";
import type { DepartmentDto } from "../types/departments";
import type { PagedResult } from "../types/pagination";
import type {
    TicketDto,
    TicketPriorityValue,
    SlaStatus,
    TicketSortBy,
    TicketStatusValue,
} from "../types/tickets";
import type { UserDto } from "../types/users";

interface TicketFilters {
    ticketNumber: string;
    search: string;
    status: TicketStatusValue | "";
    priority: TicketPriorityValue | "";
    slaStatus: SlaStatus | "";
    activeOnly: boolean;
    unassignedOnly: boolean;
    assetId: string;
    categoryId: string;
    createdByUserId: string;
    assignedTechnicianId: string;
    departmentId: string;
    createdFrom: string;
    createdTo: string;
}

const initialFilters: TicketFilters = {
    ticketNumber: "",
    search: "",
    status: "",
    priority: "",
    slaStatus: "",
    activeOnly: false,
    unassignedOnly: false,
    assetId: "",
    categoryId: "",
    createdByUserId: "",
    assignedTechnicianId: "",
    departmentId: "",
    createdFrom: "",
    createdTo: "",
};

const statusOptions: Array<{
    label: string;
    value: TicketStatusValue;
}> = [
    { label: "Açık", value: 1 },
    { label: "Atandı", value: 2 },
    { label: "İşlemde", value: 3 },
    { label: "Bekliyor", value: 4 },
    { label: "Çözüldü", value: 5 },
    { label: "Kapandı", value: 6 },
    { label: "İptal", value: 7 },
];

const priorityOptions: Array<{
    label: string;
    value: TicketPriorityValue;
}> = [
    { label: "Düşük", value: 1 },
    { label: "Orta", value: 2 },
    { label: "Yüksek", value: 3 },
    { label: "Kritik", value: 4 },
];

const slaOptions: Array<{ label: string; value: SlaStatus }> = [
    { label: "Süre İçinde", value: "OnTrack" },
    { label: "Süre Yaklaşıyor", value: "DueSoon" },
    { label: "SLA Aşıldı", value: "Breached" },
    { label: "SLA Karşılandı", value: "Met" },
    { label: "Uygulanamaz", value: "NotApplicable" },
];

const sortOptions: Array<{
    label: string;
    value: TicketSortBy;
}> = [
    { label: "Oluşturulma tarihi", value: "createdAt" },
    { label: "Talep No", value: "ticketNumber" },
    { label: "Başlık", value: "title" },
    { label: "Kategori", value: "category" },
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
    const { token, user } = useAuth();
    const isAdmin = user?.role === "Admin";
    const [searchParams] = useSearchParams();

    const [result, setResult] = useState(emptyResult);
    const [assets, setAssets] = useState<AssetDto[]>([]);
    const [categories, setCategories] = useState<TicketCategoryDto[]>([]);
    const [users, setUsers] = useState<UserDto[]>([]);
    const [departments, setDepartments] = useState<DepartmentDto[]>([]);
    const [draftFilters, setDraftFilters] =
        useState<TicketFilters>(() => getFiltersFromSearchParams(searchParams));
    const [appliedFilters, setAppliedFilters] =
        useState<TicketFilters>(() => getFiltersFromSearchParams(searchParams));
    const [sortBy, setSortBy] = useState<TicketSortBy>("createdAt");
    const [sortDescending, setSortDescending] = useState(true);
    const [pageNumber, setPageNumber] = useState(1);
    const [pageSize, setPageSize] = useState(10);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [filterOptionsError, setFilterOptionsError] =
        useState<string | null>(null);
    const [filterValidationError, setFilterValidationError] =
        useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadFilterOptions() {
            if (!token) {
                return;
            }

            const optionErrors: string[] = [];
            const [assetResult, categoryResult] = await Promise.allSettled([
                getAssets(token),
                getCategories(token, isAdmin),
            ]);

            if (cancelled) {
                return;
            }

            if (assetResult.status === "fulfilled") {
                setAssets(assetResult.value);
            } else {
                optionErrors.push("cihazlar");
            }

            if (categoryResult.status === "fulfilled") {
                setCategories(categoryResult.value);
            } else {
                optionErrors.push("kategoriler");
            }

            if (isAdmin) {
                const [userResult, departmentResult] =
                    await Promise.allSettled([
                        getUsers(token),
                        getDepartments(token),
                    ]);

                if (cancelled) {
                    return;
                }

                if (userResult.status === "fulfilled") {
                    setUsers(userResult.value);
                } else {
                    optionErrors.push("kullanıcılar");
                }

                if (departmentResult.status === "fulfilled") {
                    setDepartments(departmentResult.value);
                } else {
                    optionErrors.push("departmanlar");
                }
            } else {
                setUsers([]);
                setDepartments([]);
            }

            setFilterOptionsError(
                optionErrors.length > 0
                    ? `${optionErrors.join(", ")} filtre seçenekleri ` +
                        "yüklenemedi. Talep listesi kullanılmaya devam edilebilir."
                    : null
            );
        }

        loadFilterOptions();

        return () => {
            cancelled = true;
        };
    }, [isAdmin, token]);

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
                    status: appliedFilters.status || undefined,
                    priority: appliedFilters.priority || undefined,
                    slaStatus: appliedFilters.slaStatus || undefined,
                    activeOnly: appliedFilters.activeOnly || undefined,
                    unassignedOnly: appliedFilters.unassignedOnly || undefined,
                    assetId: appliedFilters.assetId || undefined,
                    ticketNumber:
                        appliedFilters.ticketNumber.trim() || undefined,
                    search: appliedFilters.search.trim() || undefined,
                    categoryId: appliedFilters.categoryId || undefined,
                    createdByUserId:
                        appliedFilters.createdByUserId || undefined,
                    assignedTechnicianId:
                        appliedFilters.assignedTechnicianId || undefined,
                    departmentId:
                        appliedFilters.departmentId || undefined,
                    createdFrom: appliedFilters.createdFrom
                        ? toUtcDayStart(appliedFilters.createdFrom)
                        : undefined,
                    createdTo: appliedFilters.createdTo
                        ? toUtcDayEnd(appliedFilters.createdTo)
                        : undefined,
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
        appliedFilters,
        pageNumber,
        pageSize,
        sortBy,
        sortDescending,
        token,
    ]);

    const technicianOptions = users.filter(
        (candidate) => candidate.role === "Technician"
    );
    const hasActiveFilters = Object.values(appliedFilters).some(Boolean);

    function updateDraftFilter<K extends keyof TicketFilters>(
        key: K,
        value: TicketFilters[K]
    ) {
        setDraftFilters((current) => ({
            ...current,
            [key]: value,
        }));
    }

    function applyFilters(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();

        if (
            draftFilters.createdFrom &&
            draftFilters.createdTo &&
            draftFilters.createdFrom > draftFilters.createdTo
        ) {
            setFilterValidationError(
                "Başlangıç tarihi bitiş tarihinden sonra olamaz."
            );
            return;
        }

        setFilterValidationError(null);
        setPageNumber(1);
        setAppliedFilters({ ...draftFilters });
    }

    function clearFilters() {
        setDraftFilters(initialFilters);
        setAppliedFilters(initialFilters);
        setFilterValidationError(null);
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
                            Alanları doldurduktan sonra filtreleri uygulayın.
                        </p>
                    </div>
                </div>

                {(appliedFilters.activeOnly || appliedFilters.unassignedOnly) && (
                    <div className="filter-context" role="status">
                        <strong>Dashboard kapsamı:</strong>
                        {appliedFilters.activeOnly && <span>Aktif talepler</span>}
                        {appliedFilters.unassignedOnly && <span>Atanmamış talepler</span>}
                    </div>
                )}

                <form onSubmit={applyFilters}>
                    <div className="toolbar-grid">
                        <div className="form-group">
                            <label htmlFor="ticket-search-filter">Ara</label>
                            <input
                                id="ticket-search-filter"
                                type="search"
                                value={draftFilters.search}
                                maxLength={200}
                                placeholder="Talep no, başlık veya açıklama..."
                                onChange={(event) => updateDraftFilter(
                                    "search",
                                    event.target.value
                                )}
                            />
                        </div>

                        <div className="form-group">
                            <label htmlFor="ticket-status-filter">Durum</label>
                            <select
                                id="ticket-status-filter"
                                value={draftFilters.status}
                                onChange={(event) => updateDraftFilter(
                                    "status",
                                    event.target.value
                                        ? Number(event.target.value) as
                                            TicketStatusValue
                                        : ""
                                )}
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
                                value={draftFilters.priority}
                                onChange={(event) => updateDraftFilter(
                                    "priority",
                                    event.target.value
                                        ? Number(event.target.value) as
                                            TicketPriorityValue
                                        : ""
                                )}
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
                            <label htmlFor="ticket-sla-filter">SLA</label>
                            <select
                                id="ticket-sla-filter"
                                value={draftFilters.slaStatus}
                                onChange={(event) => updateDraftFilter(
                                    "slaStatus",
                                    event.target.value as SlaStatus | ""
                                )}
                            >
                                <option value="">Tümü</option>
                                {slaOptions.map((option) => (
                                    <option key={option.value} value={option.value}>
                                        {option.label}
                                    </option>
                                ))}
                            </select>
                        </div>

                    </div>

                    <details className="advanced-filters">
                            <summary>Gelişmiş filtreler</summary>
                            <div className="toolbar-grid">
                                <div className="form-group">
                                    <label htmlFor="ticket-number-filter">Talep No</label>
                                    <input
                                        id="ticket-number-filter"
                                        type="text"
                                        value={draftFilters.ticketNumber}
                                        maxLength={15}
                                        placeholder="REQ-2026-"
                                        onChange={(event) => updateDraftFilter(
                                            "ticketNumber",
                                            event.target.value
                                        )}
                                    />
                                </div>

                                <div className="form-group">
                                    <label htmlFor="ticket-category-filter">Kategori</label>
                                    <select
                                        id="ticket-category-filter"
                                        value={draftFilters.categoryId}
                                        onChange={(event) => updateDraftFilter(
                                            "categoryId",
                                            event.target.value
                                        )}
                                    >
                                        <option value="">Tüm kategoriler</option>
                                        {categories.map((category) => (
                                            <option key={category.id} value={category.id}>
                                                {category.name}
                                                {category.isActive ? "" : " (Pasif)"}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div className="form-group">
                                    <label htmlFor="ticket-asset-filter">Cihaz</label>
                                    <select
                                        id="ticket-asset-filter"
                                        value={draftFilters.assetId}
                                        onChange={(event) => updateDraftFilter(
                                            "assetId",
                                            event.target.value
                                        )}
                                    >
                                        <option value="">Tüm cihazlar</option>
                                        {assets.map((asset) => (
                                            <option key={asset.id} value={asset.id}>
                                                {asset.name} ({asset.serialNumber})
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                {isAdmin && (
                                    <>
                                <div className="form-group">
                                    <label htmlFor="ticket-created-by-filter">
                                        Oluşturan
                                    </label>
                                    <select
                                        id="ticket-created-by-filter"
                                        value={draftFilters.createdByUserId}
                                        onChange={(event) => updateDraftFilter(
                                            "createdByUserId",
                                            event.target.value
                                        )}
                                    >
                                        <option value="">Tüm kullanıcılar</option>
                                        {users.map((candidate) => (
                                            <option
                                                key={candidate.id}
                                                value={candidate.id}
                                            >
                                                {candidate.fullName}
                                                {formatAccountStatus(candidate)}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div className="form-group">
                                    <label htmlFor="ticket-technician-filter">
                                        Atanan Teknik Personel
                                    </label>
                                    <select
                                        id="ticket-technician-filter"
                                        value={
                                            draftFilters.assignedTechnicianId
                                        }
                                        onChange={(event) => updateDraftFilter(
                                            "assignedTechnicianId",
                                            event.target.value
                                        )}
                                    >
                                        <option value="">Tüm teknisyenler</option>
                                        {technicianOptions.map((technician) => (
                                            <option
                                                key={technician.id}
                                                value={technician.id}
                                            >
                                                {technician.fullName}
                                                {formatAccountStatus(technician)}
                                            </option>
                                        ))}
                                    </select>
                                </div>

                                <div className="form-group">
                                    <label htmlFor="ticket-department-filter">
                                        Talep Sahibinin Departmanı
                                    </label>
                                    <select
                                        id="ticket-department-filter"
                                        value={draftFilters.departmentId}
                                        onChange={(event) => updateDraftFilter(
                                            "departmentId",
                                            event.target.value
                                        )}
                                    >
                                        <option value="">Tüm departmanlar</option>
                                        {departments.map((department) => (
                                            <option key={department.id} value={department.id}>
                                                {department.name}
                                                {department.isActive ? "" : " (Pasif)"}
                                            </option>
                                        ))}
                                    </select>
                                </div>
                                    </>
                                )}

                                <div className="form-group">
                                    <label htmlFor="ticket-created-from-filter">
                                        Başlangıç Tarihi
                                    </label>
                                    <input
                                        id="ticket-created-from-filter"
                                        type="date"
                                        value={draftFilters.createdFrom}
                                        onChange={(event) => updateDraftFilter(
                                            "createdFrom",
                                            event.target.value
                                        )}
                                    />
                                </div>

                                <div className="form-group">
                                    <label htmlFor="ticket-created-to-filter">
                                        Bitiş Tarihi
                                    </label>
                                    <input
                                        id="ticket-created-to-filter"
                                        type="date"
                                        value={draftFilters.createdTo}
                                        onChange={(event) => updateDraftFilter(
                                            "createdTo",
                                            event.target.value
                                        )}
                                    />
                                </div>
                            </div>
                        </details>

                    <div className="toolbar-grid ticket-list-preferences">
                        <div className="form-group">
                            <label htmlFor="ticket-sort-by">Sıralama</label>
                            <select
                                id="ticket-sort-by"
                                value={sortBy}
                                onChange={(event) => {
                                    setSortBy(event.target.value as TicketSortBy);
                                    setPageNumber(1);
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
                                value={sortDescending
                                    ? "descending"
                                    : "ascending"}
                                onChange={(event) => {
                                    setSortDescending(
                                        event.target.value === "descending"
                                    );
                                    setPageNumber(1);
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
                                    setPageNumber(1);
                                }}
                            >
                                <option value={10}>10</option>
                                <option value={25}>25</option>
                                <option value={50}>50</option>
                            </select>
                        </div>
                    </div>

                    {filterValidationError && (
                        <p className="error-state" role="alert">
                            {filterValidationError}
                        </p>
                    )}

                    <div className="form-actions filter-actions">
                        <button
                            type="submit"
                            className="button button-primary"
                            disabled={isLoading}
                        >
                            Filtreleri Uygula
                        </button>
                        <button
                            type="button"
                            className="button button-secondary"
                            onClick={clearFilters}
                            disabled={isLoading && !hasActiveFilters}
                        >
                            Filtreleri Temizle
                        </button>
                    </div>
                </form>
            </section>

            {filterOptionsError && (
                <p className="error-state" role="alert">
                    {filterOptionsError}
                </p>
            )}
            {isLoading && (
                <p className="loading-state">Ticketlar yükleniyor...</p>
            )}
            {error && (
                <p className="error-state" role="alert">{error}</p>
            )}

            {!isLoading && !error && result.items.length === 0 && (
                <div className="empty-state">
                    <p>
                        {hasActiveFilters
                            ? "Seçilen filtrelerle eşleşen talep bulunamadı."
                            : "Henüz talep bulunmuyor."}
                    </p>
                    {hasActiveFilters && (
                        <button
                            type="button"
                            className="button button-secondary button-small"
                            onClick={clearFilters}
                        >
                            Filtreleri Temizle
                        </button>
                    )}
                </div>
            )}

            {!isLoading && !error && result.items.length > 0 && (
                <div className="table-container ticket-table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Talep</th>
                                <th>Durum</th>
                                <th>Öncelik</th>
                                <th>SLA</th>
                                <th>Teknisyen</th>
                                <th>Tarih</th>
                            </tr>
                        </thead>
                        <tbody>
                            {result.items.map((ticket) => (
                                <tr key={ticket.id}>
                                    <td className="ticket-primary-cell">
                                        <Link
                                            to={`/tickets/${ticket.id}`}
                                            className="table-link ticket-number"
                                        >
                                            {ticket.ticketNumber}
                                        </Link>
                                        <Link
                                            to={`/tickets/${ticket.id}`}
                                            className="table-link ticket-title-link"
                                        >
                                            {ticket.title}
                                        </Link>
                                        <span className="ticket-cell-secondary">
                                            {ticket.categoryName} · {ticket.assetName} · {ticket.createdByFullName}
                                        </span>
                                    </td>
                                    <td>
                                        <TicketStatusBadge status={ticket.status} />
                                    </td>
                                    <td>
                                        <TicketPriorityBadge
                                            priority={ticket.priority}
                                        />
                                    </td>
                                    <td>
                                        <div className="sla-table-cell">
                                            <SlaBadge status={ticket.slaStatus} />
                                            {formatSlaRemainingTime(
                                                ticket.slaStatus,
                                                ticket.slaRemainingMinutes
                                            ) && (
                                                <span className="sla-remaining">
                                                    {formatSlaRemainingTime(
                                                        ticket.slaStatus,
                                                        ticket.slaRemainingMinutes
                                                    )}
                                                </span>
                                            )}
                                        </div>
                                    </td>
                                    <td className="ticket-assignee-cell">
                                        {ticket.assignedTechnicianFullName ?? (
                                            <span className="muted-text">
                                                Atanmadı
                                            </span>
                                        )}
                                    </td>
                                    <td className="ticket-date-cell">
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

function toUtcDayStart(value: string): string {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day, 0, 0, 0, 0).toISOString();
}

function getFiltersFromSearchParams(
    searchParams: URLSearchParams
): TicketFilters {
    const status = Number(searchParams.get("status"));
    const priority = Number(searchParams.get("priority"));
    const slaStatus = searchParams.get("slaStatus");

    return {
        ...initialFilters,
        status: isTicketStatusValue(status) ? status : "",
        priority: isTicketPriorityValue(priority) ? priority : "",
        slaStatus: isSlaStatus(slaStatus) ? slaStatus : "",
        activeOnly: searchParams.get("activeOnly") === "true",
        unassignedOnly: searchParams.get("unassignedOnly") === "true",
        categoryId: searchParams.get("categoryId") ?? "",
        assignedTechnicianId:
            searchParams.get("assignedTechnicianId") ?? "",
        departmentId: searchParams.get("departmentId") ?? "",
        createdFrom: getDateParameter(searchParams, "createdFrom"),
        createdTo: getDateParameter(searchParams, "createdTo"),
    };
}

function isTicketStatusValue(value: number): value is TicketStatusValue {
    return Number.isInteger(value) && value >= 1 && value <= 7;
}

function isTicketPriorityValue(value: number): value is TicketPriorityValue {
    return Number.isInteger(value) && value >= 1 && value <= 4;
}

function isSlaStatus(value: string | null): value is SlaStatus {
    return value !== null && slaOptions.some((option) => option.value === value);
}

function getDateParameter(
    searchParams: URLSearchParams,
    name: "createdFrom" | "createdTo"
): string {
    const value = searchParams.get(name) ?? "";
    return /^\d{4}-\d{2}-\d{2}$/.test(value) ? value : "";
}

function toUtcDayEnd(value: string): string {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day, 23, 59, 59, 999).toISOString();
}

function formatAccountStatus(user: UserDto): string {
    if (user.accountStatus === "Active") {
        return "";
    }

    return user.accountStatus === "PendingInvitation"
        ? " (Davet Bekliyor)"
        : " (Pasif)";
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
