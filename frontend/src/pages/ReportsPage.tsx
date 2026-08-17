import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";
import { useNavigate } from "react-router-dom";

import { getCategories } from "../api/categoriesApi";
import { getDepartments } from "../api/departmentsApi";
import { ApiError } from "../api/httpClient";
import {
    downloadTicketReport,
    getReportOverview,
} from "../api/reportsApi";
import { getUsers } from "../api/usersApi";
import { useAuth } from "../auth/useAuth";

import type { TicketCategoryDto } from "../types/categories";
import type { DepartmentDto } from "../types/departments";
import type {
    ReportDistributionItemDto,
    ReportFilterQuery,
    ReportOverviewDto,
    TechnicianPerformanceDto,
} from "../types/reports";
import type { UserDto } from "../types/users";

interface ReportFilters {
    createdFrom: string;
    createdTo: string;
    categoryId: string;
    departmentId: string;
    assignedTechnicianId: string;
}

const emptyFilters: ReportFilters = {
    createdFrom: "",
    createdTo: "",
    categoryId: "",
    departmentId: "",
    assignedTechnicianId: "",
};

export function ReportsPage() {
    const { token } = useAuth();
    const navigate = useNavigate();
    const [report, setReport] = useState<ReportOverviewDto | null>(null);
    const [categories, setCategories] = useState<TicketCategoryDto[]>([]);
    const [departments, setDepartments] = useState<DepartmentDto[]>([]);
    const [technicians, setTechnicians] = useState<UserDto[]>([]);
    const [draftFilters, setDraftFilters] = useState(emptyFilters);
    const [filters, setFilters] = useState(emptyFilters);
    const [isLoading, setIsLoading] = useState(true);
    const [isExporting, setIsExporting] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [filterError, setFilterError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadOptions() {
            if (!token) {
                return;
            }

            try {
                const [categoryResult, departmentResult, userResult] =
                    await Promise.all([
                        getCategories(token, true),
                        getDepartments(token),
                        getUsers(token),
                    ]);

                if (!cancelled) {
                    setCategories(categoryResult);
                    setDepartments(departmentResult);
                    setTechnicians(userResult.filter(
                        (user) => user.role === "Technician"
                    ));
                }
            } catch (caughtError) {
                if (!cancelled) {
                    setError(getErrorMessage(
                        caughtError,
                        "Rapor filtre seçenekleri yüklenemedi."
                    ));
                }
            }
        }

        loadOptions();
        return () => {
            cancelled = true;
        };
    }, [token]);

    useEffect(() => {
        let cancelled = false;

        async function loadReport() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setError(null);
                const result = await getReportOverview(
                    token,
                    toReportQuery(filters)
                );

                if (!cancelled) {
                    setReport(result);
                }
            } catch (caughtError) {
                if (!cancelled) {
                    setError(getErrorMessage(
                        caughtError,
                        "Rapor verileri yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadReport();
        return () => {
            cancelled = true;
        };
    }, [filters, token]);

    function applyFilters(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();

        if (
            draftFilters.createdFrom &&
            draftFilters.createdTo &&
            draftFilters.createdFrom > draftFilters.createdTo
        ) {
            setFilterError("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
            return;
        }

        setFilterError(null);
        setFilters({ ...draftFilters });
    }

    function clearFilters() {
        setDraftFilters(emptyFilters);
        setFilters(emptyFilters);
        setFilterError(null);
    }

    async function exportCsv() {
        if (!token || isExporting) {
            return;
        }

        try {
            setIsExporting(true);
            setError(null);
            const download = await downloadTicketReport(
                token,
                toReportQuery(filters)
            );
            const objectUrl = URL.createObjectURL(download.blob);

            try {
                const link = document.createElement("a");
                link.href = objectUrl;
                link.download = download.fileName;
                document.body.appendChild(link);
                link.click();
                link.remove();
            } finally {
                URL.revokeObjectURL(objectUrl);
            }
        } catch (caughtError) {
            setError(getErrorMessage(caughtError, "CSV raporu indirilemedi."));
        } finally {
            setIsExporting(false);
        }
    }

    return (
        <div className="page reports-page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Raporlar</h1>
                    <p className="page-description">
                        Talep performansı, SLA uyumu ve teknik personel iş yükü
                        gibi operasyonel verileri analiz edin.
                    </p>
                </div>
                <div className="page-header-actions">
                    <button
                        type="button"
                        className="button button-primary"
                        disabled={isExporting || isLoading}
                        onClick={exportCsv}
                    >
                        {isExporting ? "İndiriliyor..." : "CSV İndir"}
                    </button>
                </div>
            </header>

            <section className="card" aria-labelledby="report-filters-title">
                <div className="card-header">
                    <h2 id="report-filters-title">Rapor Filtreleri</h2>
                </div>
                <form onSubmit={applyFilters}>
                    <div className="report-filter-grid">
                        <DateFilter
                            id="report-created-from"
                            label="Başlangıç tarihi"
                            value={draftFilters.createdFrom}
                            onChange={(value) => setDraftFilters({
                                ...draftFilters,
                                createdFrom: value,
                            })}
                        />
                        <DateFilter
                            id="report-created-to"
                            label="Bitiş tarihi"
                            value={draftFilters.createdTo}
                            onChange={(value) => setDraftFilters({
                                ...draftFilters,
                                createdTo: value,
                            })}
                        />
                        <SelectFilter
                            id="report-category"
                            label="Kategori"
                            value={draftFilters.categoryId}
                            options={categories.map((item) => ({
                                value: item.id,
                                label: item.name,
                            }))}
                            onChange={(value) => setDraftFilters({
                                ...draftFilters,
                                categoryId: value,
                            })}
                        />
                        <SelectFilter
                            id="report-department"
                            label="Departman"
                            value={draftFilters.departmentId}
                            options={departments.map((item) => ({
                                value: item.id,
                                label: item.name,
                            }))}
                            onChange={(value) => setDraftFilters({
                                ...draftFilters,
                                departmentId: value,
                            })}
                        />
                        <SelectFilter
                            id="report-technician"
                            label="Teknik Personel"
                            value={draftFilters.assignedTechnicianId}
                            options={technicians.map((item) => ({
                                value: item.id,
                                label: item.fullName,
                            }))}
                            onChange={(value) => setDraftFilters({
                                ...draftFilters,
                                assignedTechnicianId: value,
                            })}
                        />
                    </div>
                    {filterError && (
                        <p className="error-state" role="alert">{filterError}</p>
                    )}
                    <div className="form-actions">
                        <button className="button button-primary" disabled={isLoading}>
                            Uygula
                        </button>
                        <button
                            type="button"
                            className="button button-secondary"
                            disabled={isLoading}
                            onClick={clearFilters}
                        >
                            Temizle
                        </button>
                    </div>
                </form>
            </section>

            {isLoading && <p className="loading-state" role="status">Rapor yükleniyor...</p>}
            {error && <p className="error-state" role="alert">{error}</p>}

            {!isLoading && !error && report && (
                <ReportContent
                    report={report}
                    onDrillDown={(drillDown) => {
                        const query = new URLSearchParams({
                            ...getReportScope(filters),
                            ...drillDown,
                        });
                        navigate(`/tickets?${query.toString()}`);
                    }}
                />
            )}
        </div>
    );
}

function ReportContent({
    report,
    onDrillDown,
}: {
    report: ReportOverviewDto;
    onDrillDown: (query: Record<string, string>) => void;
}) {
    const [selectedTechnician, setSelectedTechnician] =
        useState<TechnicianPerformanceDto | null>(null);
    const primarySummaryCards = [
        ["Toplam Talep", report.summary.totalTickets, {}],
        ["Aktif", report.summary.activeTickets, null],
        ["Çözülen", report.summary.resolvedTickets, { status: "5" }],
        ["Kritik", report.summary.criticalTickets, { priority: "4" }],
        ["SLA Uyum Oranı", `${formatNumber(report.summary.slaComplianceRate)}%`, null],
    ] as const;
    const secondarySummaryCards = [
        ["Kapatılan", report.summary.closedTickets, { status: "6" }],
        ["İptal", report.summary.cancelledTickets, { status: "7" }],
        ["SLA Karşılanan", report.summary.slaMetCount, { slaStatus: "Met" }],
        ["SLA Aşılan", report.summary.slaBreachedCount, { slaStatus: "Breached" }],
    ] as const;
    const maximumTrend = Math.max(
        1,
        ...report.dailyCreationTrend.map((item) => item.count)
    );

    return (
        <>
            <section aria-labelledby="report-summary-title">
                <h2 id="report-summary-title" className="visually-hidden">Rapor özeti</h2>
                <div className="report-summary-grid">
                    {primarySummaryCards.map(([label, value, drillDown], index) => (
                        <button
                            type="button"
                            className={`summary-card report-metric-card ${index === 4 ? "metric-success" : "metric-primary"}`}
                            key={label}
                            disabled={!drillDown}
                            onClick={() => drillDown && onDrillDown(drillDown)}
                        >
                            <span className="summary-label">{label}</span>
                            <strong className="summary-value">{value}</strong>
                        </button>
                    ))}
                </div>
                <div className="report-summary-secondary">
                    {secondarySummaryCards.map(([label, value, drillDown]) => (
                        <button
                            type="button"
                            className="summary-card report-metric-card"
                            key={label}
                            onClick={() => onDrillDown(drillDown)}
                        >
                            <span className="summary-label">{label}</span>
                            <strong className="summary-value">{value}</strong>
                        </button>
                    ))}
                </div>
            </section>

            <section className="report-distribution-grid" aria-label="Talep dağılımları">
                <DistributionCard
                    title="Duruma Göre"
                    items={report.byStatus}
                    getDrillDown={(key) => getStatusDrillDown(key)}
                    onDrillDown={onDrillDown}
                />
                <DistributionCard
                    title="Önceliğe Göre"
                    items={report.byPriority}
                    getDrillDown={(key) => getPriorityDrillDown(key)}
                    onDrillDown={onDrillDown}
                />
                <DistributionCard
                    title="Kategoriye Göre"
                    items={report.byCategory}
                    getDrillDown={(key) => ({ categoryId: key })}
                    onDrillDown={onDrillDown}
                />
            </section>

            <section className="card" aria-labelledby="report-trend-title">
                <div className="card-header"><h2 id="report-trend-title">Günlük Talep Trendi</h2></div>
                {report.dailyCreationTrend.length === 0 ? (
                    <p className="empty-state">Seçilen aralıkta talep bulunmuyor.</p>
                ) : (
                    <div className="trend-list">
                        {report.dailyCreationTrend.map((item) => (
                            <div
                                className="trend-item"
                                key={item.date}
                                title="UTC gün sınırı nedeniyle bu grafik doğrudan filtrelenmez."
                            >
                                <span
                                    className="trend-bar"
                                    style={{ height: `${Math.max(5, item.count / maximumTrend * 120)}px` }}
                                    aria-hidden="true"
                                />
                                <strong>{item.count}</strong>
                                <span>{formatDate(item.date)}</span>
                            </div>
                        ))}
                    </div>
                )}
            </section>

            <section className="card" aria-labelledby="technician-performance-title">
                <div className="card-header"><h2 id="technician-performance-title">Teknik Personel Performansı</h2></div>
                {report.technicianPerformance.length === 0 ? (
                    <p className="empty-state">Gösterilecek teknik personel verisi bulunmuyor.</p>
                ) : (
                    <div className="table-container report-table-container">
                        <table>
                            <thead><tr>
                                <th>Teknik Personel</th><th>Atanan</th><th>Aktif</th>
                                <th>Çözülen/Kapatılan</th><th>SLA Karşılanan</th>
                                <th>SLA Aşılan</th><th>SLA Uyum Oranı</th>
                            </tr></thead>
                            <tbody>{report.technicianPerformance.map((item) => (
                                <tr
                                    className="report-interactive-row"
                                    key={item.technicianId}
                                    tabIndex={0}
                                    onClick={() => setSelectedTechnician(item)}
                                    onKeyDown={(event) => {
                                        if (event.key === "Enter" || event.key === " ") {
                                            event.preventDefault();
                                            setSelectedTechnician(item);
                                        }
                                    }}
                                >
                                    <td>{item.fullName}</td><td>{item.assignedCount}</td>
                                    <td>{item.activeCount}</td><td>{item.resolvedOrClosedCount}</td>
                                    <td>{item.slaMetCount}</td><td>{item.slaBreachedCount}</td>
                                    <td>{formatNumber(item.slaComplianceRate)}%</td>
                                </tr>
                            ))}</tbody>
                        </table>
                    </div>
                )}
            </section>

            {selectedTechnician && (
                <TechnicianDetailDrawer
                    technician={selectedTechnician}
                    onClose={() => setSelectedTechnician(null)}
                    onViewTickets={() => onDrillDown({
                        assignedTechnicianId: selectedTechnician.technicianId,
                    })}
                />
            )}
        </>
    );
}

function DistributionCard({
    title,
    items,
    getDrillDown,
    onDrillDown,
}: {
    title: string;
    items: ReportDistributionItemDto[];
    getDrillDown: (key: string) => Record<string, string> | null;
    onDrillDown: (query: Record<string, string>) => void;
}) {
    const maximum = Math.max(0, ...items.map((item) => item.count));

    return (
        <article className="card distribution-card">
            <h2>{title}</h2>
            {items.length === 0 ? (
                <p className="empty-state">Veri bulunmuyor.</p>
            ) : (
                <ul className="distribution-list">
                    {items.map((item) => {
                        const drillDown = getDrillDown(item.key);

                        return (
                        <li key={item.key}>
                            <button
                                type="button"
                                className="distribution-action"
                                disabled={!drillDown}
                                onClick={() => drillDown && onDrillDown(drillDown)}
                            >
                            <div className="distribution-label">
                                <span>{item.label}</span>
                                <strong>{item.count}</strong>
                            </div>
                            <div className="distribution-track" aria-hidden="true">
                                <span style={{ width: `${maximum ? item.count / maximum * 100 : 0}%` }} />
                            </div>
                            </button>
                        </li>
                        );
                    })}
                </ul>
            )}
        </article>
    );
}

function TechnicianDetailDrawer({
    technician,
    onClose,
    onViewTickets,
}: {
    technician: TechnicianPerformanceDto;
    onClose: () => void;
    onViewTickets: () => void;
}) {
    useEffect(() => {
        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === "Escape") {
                onClose();
            }
        }

        document.addEventListener("keydown", handleKeyDown);
        return () => document.removeEventListener("keydown", handleKeyDown);
    }, [onClose]);

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
                className="detail-drawer technician-detail-drawer"
                role="dialog"
                aria-modal="true"
                aria-labelledby="technician-detail-title"
            >
                <header className="detail-drawer-header">
                    <div>
                        <span className="eyebrow">Teknik personel</span>
                        <h2 id="technician-detail-title">{technician.fullName}</h2>
                    </div>
                    <button
                        type="button"
                        className="icon-button"
                        aria-label="Detayı kapat"
                        onClick={onClose}
                    >
                        ×
                    </button>
                </header>

                <dl className="technician-metric-list">
                    <DetailMetric label="Atanan" value={technician.assignedCount} />
                    <DetailMetric label="Aktif" value={technician.activeCount} />
                    <DetailMetric
                        label="Çözülen/Kapatılan"
                        value={technician.resolvedOrClosedCount}
                    />
                    <DetailMetric label="SLA Karşılanan" value={technician.slaMetCount} />
                    <DetailMetric label="SLA Aşılan" value={technician.slaBreachedCount} />
                    <DetailMetric
                        label="SLA Uyum Oranı"
                        value={`${formatNumber(technician.slaComplianceRate)}%`}
                    />
                </dl>

                <button
                    type="button"
                    className="button button-primary"
                    onClick={onViewTickets}
                >
                    Taleplerini Gör
                </button>
            </aside>
        </div>
    );
}

function DetailMetric({ label, value }: { label: string; value: string | number }) {
    return (
        <div>
            <dt>{label}</dt>
            <dd>{value}</dd>
        </div>
    );
}

function getReportScope(filters: ReportFilters): Record<string, string> {
    return Object.fromEntries(
        Object.entries(filters).filter(([, value]) => value)
    );
}

function getStatusDrillDown(key: string): Record<string, string> | null {
    const values: Record<string, string> = {
        Open: "1",
        Assigned: "2",
        InProgress: "3",
        Waiting: "4",
        Resolved: "5",
        Closed: "6",
        Cancelled: "7",
    };

    return values[key] ? { status: values[key] } : null;
}

function getPriorityDrillDown(key: string): Record<string, string> | null {
    const values: Record<string, string> = {
        Low: "1",
        Medium: "2",
        High: "3",
        Critical: "4",
    };

    return values[key] ? { priority: values[key] } : null;
}

function DateFilter(props: {
    id: string;
    label: string;
    value: string;
    onChange: (value: string) => void;
}) {
    return <div className="form-group"><label htmlFor={props.id}>{props.label}</label><input id={props.id} type="date" value={props.value} onChange={(event) => props.onChange(event.target.value)} /></div>;
}

function SelectFilter(props: {
    id: string;
    label: string;
    value: string;
    options: Array<{ value: string; label: string }>;
    onChange: (value: string) => void;
}) {
    return <div className="form-group"><label htmlFor={props.id}>{props.label}</label><select id={props.id} value={props.value} onChange={(event) => props.onChange(event.target.value)}><option value="">Tümü</option>{props.options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></div>;
}

function toReportQuery(filters: ReportFilters): ReportFilterQuery {
    return {
        createdFrom: filters.createdFrom ? toUtcStart(filters.createdFrom) : undefined,
        createdTo: filters.createdTo ? toUtcEnd(filters.createdTo) : undefined,
        categoryId: filters.categoryId || undefined,
        departmentId: filters.departmentId || undefined,
        assignedTechnicianId: filters.assignedTechnicianId || undefined,
    };
}

function toUtcStart(value: string): string {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day).toISOString();
}

function toUtcEnd(value: string): string {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day, 23, 59, 59, 999).toISOString();
}

function formatDate(value: string): string {
    return new Date(`${value}T00:00:00`).toLocaleDateString("tr-TR");
}

function formatNumber(value: number): string {
    return new Intl.NumberFormat("tr-TR", { maximumFractionDigits: 2 }).format(value || 0);
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof ApiError ? error.message : fallback;
}
