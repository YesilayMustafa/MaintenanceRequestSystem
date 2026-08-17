import { useEffect, useMemo, useState, type SubmitEvent } from "react";
import { Link } from "react-router-dom";

import { getCategories } from "../api/categoriesApi";
import { getDepartments } from "../api/departmentsApi";
import { ApiError } from "../api/httpClient";
import { getTicketTimeline } from "../api/timelineApi";
import { getUsers } from "../api/usersApi";
import { useAuth } from "../auth/useAuth";
import { TicketPriorityBadge, TicketStatusBadge } from "../components/TicketBadges";
import {
    getTimelineBarClass,
    getTimelineBarStyle,
    getTimelineTooltip,
} from "../utils/timelinePresentation";
import { getWeekRange, shiftWeek } from "../utils/weekRange";

import type { TicketCategoryDto } from "../types/categories";
import type { DepartmentDto } from "../types/departments";
import type { SlaStatus, TicketPriorityValue, TicketStatusValue } from "../types/tickets";
import type { TicketTimelineItemDto } from "../types/timeline";
import type { UserDto } from "../types/users";

interface TimelineFilters {
    status: TicketStatusValue | "";
    priority: TicketPriorityValue | "";
    categoryId: string;
    slaStatus: SlaStatus | "";
    assignedTechnicianId: string;
    departmentId: string;
}

const emptyFilters: TimelineFilters = {
    status: "",
    priority: "",
    categoryId: "",
    slaStatus: "",
    assignedTechnicianId: "",
    departmentId: "",
};

const statusOptions = [
    [1, "Açık"], [2, "Atandı"], [3, "İşlemde"], [4, "Bekliyor"],
    [5, "Çözüldü"], [6, "Kapandı"], [7, "İptal"],
] as const;

const priorityOptions = [
    [1, "Düşük"], [2, "Orta"], [3, "Yüksek"], [4, "Kritik"],
] as const;

const slaOptions: Array<[SlaStatus, string]> = [
    ["OnTrack", "Süre İçinde"],
    ["DueSoon", "Süresi Yaklaşıyor"],
    ["Breached", "SLA Aşıldı"],
    ["Met", "SLA Karşılandı"],
    ["NotApplicable", "Uygulanamaz"],
];

export function TimelinePage() {
    const { token, user } = useAuth();
    const isAdmin = user?.role === "Admin";
    const [anchor, setAnchor] = useState(() => new Date());
    const [items, setItems] = useState<TicketTimelineItemDto[]>([]);
    const [categories, setCategories] = useState<TicketCategoryDto[]>([]);
    const [departments, setDepartments] = useState<DepartmentDto[]>([]);
    const [technicians, setTechnicians] = useState<UserDto[]>([]);
    const [draftFilters, setDraftFilters] = useState(emptyFilters);
    const [filters, setFilters] = useState(emptyFilters);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const week = useMemo(() => getWeekRange(anchor), [anchor]);
    const rangeFrom = week.start.toISOString();
    const rangeTo = week.end.toISOString();

    useEffect(() => {
        let cancelled = false;

        async function loadOptions() {
            if (!token) return;

            try {
                const categoryResult = await getCategories(token, isAdmin);
                if (cancelled) return;
                setCategories(categoryResult);

                if (isAdmin) {
                    const [departmentResult, userResult] = await Promise.all([
                        getDepartments(token),
                        getUsers(token),
                    ]);
                    if (cancelled) return;
                    setDepartments(departmentResult);
                    setTechnicians(userResult.filter(
                        (candidate) => candidate.role === "Technician"
                    ));
                }
            } catch (caughtError) {
                if (!cancelled) {
                    setError(getErrorMessage(
                        caughtError,
                        "Zaman çizelgesi filtreleri yüklenemedi."
                    ));
                }
            }
        }

        loadOptions();
        return () => { cancelled = true; };
    }, [isAdmin, token]);

    useEffect(() => {
        let cancelled = false;

        async function loadTimeline() {
            if (!token) return;

            try {
                setIsLoading(true);
                setError(null);
                const result = await getTicketTimeline(token, {
                    from: rangeFrom,
                    to: rangeTo,
                    status: filters.status || undefined,
                    priority: filters.priority || undefined,
                    categoryId: filters.categoryId || undefined,
                    slaStatus: filters.slaStatus || undefined,
                    assignedTechnicianId:
                        filters.assignedTechnicianId || undefined,
                    departmentId: filters.departmentId || undefined,
                });
                if (!cancelled) setItems(result);
            } catch (caughtError) {
                if (!cancelled) {
                    setError(getErrorMessage(
                        caughtError,
                        "Talep zaman çizelgesi yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }

        loadTimeline();
        return () => { cancelled = true; };
    }, [filters, rangeFrom, rangeTo, token]);

    function applyFilters(event: SubmitEvent<HTMLFormElement>) {
        event.preventDefault();
        setFilters({ ...draftFilters });
    }

    function clearFilters() {
        setDraftFilters(emptyFilters);
        setFilters(emptyFilters);
    }

    return (
        <div className="page timeline-page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Talep Zaman Çizelgesi</h1>
                    <p className="page-description">
                        Taleplerin planlanan SLA pencerelerini haftalık görünümde izleyin.
                    </p>
                </div>
                <div className="timeline-navigation" aria-label="Hafta navigasyonu">
                    <button type="button" onClick={() => setAnchor(
                        (current) => shiftWeek(current, -1)
                    )}>Önceki Hafta</button>
                    <button type="button" onClick={() => setAnchor(new Date())}>
                        Bu Hafta
                    </button>
                    <button type="button" onClick={() => setAnchor(
                        (current) => shiftWeek(current, 1)
                    )}>Sonraki Hafta</button>
                </div>
            </header>

            <div className="timeline-range-label" role="status">
                {formatRange(week.start, week.end)}
            </div>

            <div className="timeline-legend" aria-label="Zaman çizelgesi açıklaması">
                <strong>Öncelik:</strong>
                <span><i className="timeline-legend-swatch timeline-priority-critical" />Kritik</span>
                <span><i className="timeline-legend-swatch timeline-priority-high" />Yüksek</span>
                <span><i className="timeline-legend-swatch timeline-priority-medium" />Orta</span>
                <span><i className="timeline-legend-swatch timeline-priority-low" />Düşük</span>
                <span><i className="timeline-legend-swatch timeline-legend-breached" />SLA Aşıldı</span>
            </div>

            <section className="card timeline-filter-card" aria-labelledby="timeline-filter-title">
                <div className="card-header"><h2 id="timeline-filter-title">Filtreler</h2></div>
                <form onSubmit={applyFilters}>
                    <div className="timeline-filter-grid">
                        <SelectFilter label="Durum" value={draftFilters.status} onChange={(value) => setDraftFilters({ ...draftFilters, status: value ? Number(value) as TicketStatusValue : "" })} options={statusOptions.map(([value, label]) => ({ value: String(value), label }))} />
                        <SelectFilter label="Öncelik" value={draftFilters.priority} onChange={(value) => setDraftFilters({ ...draftFilters, priority: value ? Number(value) as TicketPriorityValue : "" })} options={priorityOptions.map(([value, label]) => ({ value: String(value), label }))} />
                        <SelectFilter label="Kategori" value={draftFilters.categoryId} onChange={(value) => setDraftFilters({ ...draftFilters, categoryId: value })} options={categories.map((item) => ({ value: item.id, label: item.name }))} />
                        <SelectFilter label="SLA" value={draftFilters.slaStatus} onChange={(value) => setDraftFilters({ ...draftFilters, slaStatus: value as SlaStatus | "" })} options={slaOptions.map(([value, label]) => ({ value, label }))} />
                        {isAdmin && <SelectFilter label="Teknik Personel" value={draftFilters.assignedTechnicianId} onChange={(value) => setDraftFilters({ ...draftFilters, assignedTechnicianId: value })} options={technicians.map((item) => ({ value: item.id, label: item.fullName }))} />}
                        {isAdmin && <SelectFilter label="Departman" value={draftFilters.departmentId} onChange={(value) => setDraftFilters({ ...draftFilters, departmentId: value })} options={departments.map((item) => ({ value: item.id, label: item.name }))} />}
                    </div>
                    <div className="form-actions">
                        <button className="button button-primary" disabled={isLoading}>Uygula</button>
                        <button type="button" className="button button-secondary" onClick={clearFilters} disabled={isLoading}>Temizle</button>
                    </div>
                </form>
            </section>

            {isLoading && <p className="loading-state">Zaman çizelgesi yükleniyor...</p>}
            {error && <p className="error-state" role="alert">{error}</p>}
            {!isLoading && !error && items.length === 0 && (
                <p className="empty-state">Bu hafta için görüntülenecek talep bulunamadı.</p>
            )}
            {!isLoading && !error && items.length > 0 && (
                <TimelineGrid items={items} week={week} />
            )}
        </div>
    );
}

function TimelineGrid({
    items,
    week,
}: {
    items: TicketTimelineItemDto[];
    week: ReturnType<typeof getWeekRange>;
}) {
    return (
        <section className="timeline-scroll" aria-label="Haftalık talep zaman çizelgesi">
            <div className="timeline-grid">
                <div className="timeline-grid-header">
                    <strong className="timeline-ticket-heading">Talep</strong>
                    <div className="timeline-days timeline-day-headings">
                        {week.days.map((day) => (
                            <div className={isToday(day) ? "is-today" : ""} key={day.toISOString()}>
                                <span>{day.toLocaleDateString("tr-TR", { weekday: "short" })}</span>
                                <strong>{day.getDate()}</strong>
                            </div>
                        ))}
                    </div>
                </div>
                {items.map((item) => (
                    <div className="timeline-grid-row" key={item.id}>
                        <div className="timeline-ticket-label">
                            <Link to={`/tickets/${item.id}`}>{item.ticketNumber}</Link>
                            <span title={item.title}>{item.title}</span>
                            <div><TicketStatusBadge status={item.status} /><TicketPriorityBadge priority={item.priority} /></div>
                        </div>
                        <div className="timeline-days timeline-track">
                            {week.days.map((day) => <span key={day.toISOString()} />)}
                            <TimelineBar item={item} week={week} />
                        </div>
                    </div>
                ))}
            </div>
        </section>
    );
}

function TimelineBar({ item, week }: { item: TicketTimelineItemDto; week: ReturnType<typeof getWeekRange> }) {
    const style = getTimelineBarStyle(item, week.start, week.end);

    if (!style) return null;

    return (
        <Link
            to={`/tickets/${item.id}`}
            className={getTimelineBarClass("timeline-bar", item)}
            style={style}
            title={getTimelineTooltip(item)}
            aria-label={`${item.ticketNumber}, ${item.title}, ${item.status}`}
        />
    );
}

function SelectFilter({ label, value, options, onChange }: { label: string; value: string | number; options: Array<{ value: string; label: string }>; onChange: (value: string) => void }) {
    return <label className="form-group"><span>{label}</span><select value={value} onChange={(event) => onChange(event.target.value)}><option value="">Tümü</option>{options.map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select></label>;
}

function formatRange(start: Date, end: Date): string {
    return `${start.toLocaleDateString("tr-TR", { day: "2-digit", month: "long" })} — ${end.toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" })}`;
}

function isToday(date: Date): boolean {
    const today = new Date();
    return date.toDateString() === today.toDateString();
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof ApiError ? error.message : fallback;
}
