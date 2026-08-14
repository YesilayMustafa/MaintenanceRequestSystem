import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";

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
                <ReportContent report={report} />
            )}
        </div>
    );
}

function ReportContent({ report }: { report: ReportOverviewDto }) {
    const summaryCards = [
        ["Toplam Talep", report.summary.totalTickets],
        ["Aktif", report.summary.activeTickets],
        ["Çözülen", report.summary.resolvedTickets],
        ["Kapatılan", report.summary.closedTickets],
        ["İptal", report.summary.cancelledTickets],
        ["Kritik", report.summary.criticalTickets],
        ["SLA Karşılanan", report.summary.slaMetCount],
        ["SLA Aşılan", report.summary.slaBreachedCount],
        ["SLA Uyum Oranı", `${formatNumber(report.summary.slaComplianceRate)}%`],
    ] as const;

    return (
        <>
            <section aria-labelledby="report-summary-title">
                <h2 id="report-summary-title" className="visually-hidden">Rapor özeti</h2>
                <div className="report-summary-grid">
                    {summaryCards.map(([label, value]) => (
                        <article className="summary-card" key={label}>
                            <span className="summary-label">{label}</span>
                            <strong className="summary-value">{value}</strong>
                        </article>
                    ))}
                </div>
            </section>

            <section className="report-distribution-grid" aria-label="Talep dağılımları">
                <DistributionCard title="Duruma Göre" items={report.byStatus} />
                <DistributionCard title="Önceliğe Göre" items={report.byPriority} />
                <DistributionCard title="Kategoriye Göre" items={report.byCategory} />
            </section>

            <section className="card" aria-labelledby="report-trend-title">
                <div className="card-header"><h2 id="report-trend-title">Günlük Talep Trendi</h2></div>
                {report.dailyCreationTrend.length === 0 ? (
                    <p className="empty-state">Seçilen aralıkta talep bulunmuyor.</p>
                ) : (
                    <div className="trend-list">
                        {report.dailyCreationTrend.map((item) => (
                            <div className="trend-item" key={item.date}>
                                <span>{formatDate(item.date)}</span>
                                <strong>{item.count} talep</strong>
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
                                <tr key={item.technicianId}>
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
        </>
    );
}

function DistributionCard({
    title,
    items,
}: {
    title: string;
    items: ReportDistributionItemDto[];
}) {
    const maximum = Math.max(0, ...items.map((item) => item.count));

    return (
        <article className="card distribution-card">
            <h2>{title}</h2>
            {items.length === 0 ? (
                <p className="empty-state">Veri bulunmuyor.</p>
            ) : (
                <ul className="distribution-list">
                    {items.map((item) => (
                        <li key={item.key}>
                            <div className="distribution-label">
                                <span>{item.label}</span><strong>{item.count}</strong>
                            </div>
                            <div className="distribution-track" aria-hidden="true">
                                <span style={{ width: `${maximum ? item.count / maximum * 100 : 0}%` }} />
                            </div>
                        </li>
                    ))}
                </ul>
            )}
        </article>
    );
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
