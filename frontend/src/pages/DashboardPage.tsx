import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";

import { getDashboard } from "../api/dashboardApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import {
    TicketPriorityBadge,
    TicketStatusBadge,
} from "../components/TicketBadges";
import { Icon, type IconName } from "../components/Icon";
import { WeeklyTimelinePreview } from "../components/WeeklyTimelinePreview";

import type { DashboardDto } from "../types/dashboard";
import type { UserRole } from "../types/auth";

const roleDescriptions: Record<UserRole, string> = {
    Employee: "Taleplerinizin güncel durumunu takip edin.",
    Technician: "Size atanan teknik taleplerin durumunu takip edin.",
    Admin: "Sistem genelindeki bakım ve arıza süreçlerini takip edin.",
};

export function DashboardPage() {
    const { token, user } = useAuth();
    const navigate = useNavigate();
    const [dashboard, setDashboard] = useState<DashboardDto | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadDashboard() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setError(null);
                const result = await getDashboard(token);

                if (!cancelled) {
                    setDashboard(result);
                }
            } catch (error) {
                if (!cancelled) {
                    setError(error instanceof ApiError
                        ? error.message
                        : "Genel bakış bilgileri yüklenemedi.");
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadDashboard();

        return () => {
            cancelled = true;
        };
    }, [token]);

    return (
        <div className="page dashboard-page">
            <header className="page-header dashboard-header">
                <div>
                    <h1 className="page-title">Genel Bakış</h1>
                    <p className="page-description">
                        {user ? roleDescriptions[user.role] : "Talep süreçlerini takip edin."}
                    </p>
                </div>

                {user && (
                    <div className="page-header-actions">
                        <Link className="button button-primary" to="/tickets/new">
                            Yeni Talep Oluştur
                        </Link>
                    </div>
                )}
            </header>

            {isLoading && (
                <p className="loading-state" role="status">
                    Genel bakış yükleniyor...
                </p>
            )}

            {error && <p className="error-state" role="alert">{error}</p>}

            {!isLoading && !error && dashboard && (
                <>
                    <SummaryCards
                        dashboard={dashboard}
                        role={user?.role}
                        onDrillDown={(query) => navigateToTickets(navigate, query)}
                    />

                    {dashboard.admin && (
                        <AdminOverview
                            dashboard={dashboard.admin}
                            onDrillDown={(query) => navigateToTickets(navigate, query)}
                        />
                    )}

                    {token && <WeeklyTimelinePreview token={token} />}

                    <RecentTickets tickets={dashboard.recentTickets} />
                </>
            )}
        </div>
    );
}

interface SummaryCardsProps {
    dashboard: DashboardDto;
    role: UserRole | undefined;
    onDrillDown: (query: Record<string, string>) => void;
}

function SummaryCards({ dashboard, role, onDrillDown }: SummaryCardsProps) {
    const cards = role === "Technician"
        ? [
            ["Toplam Atanan", dashboard.totalCount, "Kapsamınızdaki tüm talepler", "ticket", "metric-primary", {}],
            ["İşlemde", dashboard.inProgressCount, "Üzerinde çalışılan", "activity", "metric-progress", { status: "3" }],
            ["Kritik Aktif", dashboard.criticalActiveCount, "Öncelikli müdahale gereken", "alert", "metric-warning", { activeOnly: "true", priority: "4" }],
            ["SLA Aşılan", dashboard.slaBreachedCount, "Hedef süresi geçen", "clock", "metric-danger", { slaStatus: "Breached" }],
            ["Bekleyen", dashboard.waitingCount, "Dış aksiyon bekleyen", "clock", "metric-warning", { status: "4" }],
            ["Çözülen", dashboard.resolvedCount, "Çözümü tamamlanan", "check", "metric-success", { status: "5" }],
            ["Süresi Yaklaşan", dashboard.slaDueSoonCount, "SLA hedefinin son bölümünde", "alert", "metric-warning", { slaStatus: "DueSoon" }],
            ["Atanan", dashboard.assignedCount, "Henüz işleme alınmayan", "users", "metric-neutral", { status: "2" }],
        ] as const
        : [
            [role === "Admin" ? "Aktif Süreç" : "Toplam", role === "Admin" ? dashboard.activeCount : dashboard.totalCount, "Kapsamınızdaki güncel talepler", "ticket", "metric-primary", role === "Admin" ? { activeOnly: "true" } : {}],
            ["İşlemde", dashboard.inProgressCount, "Üzerinde çalışılan", "activity", "metric-progress", { status: "3" }],
            ["Kritik Aktif", dashboard.criticalActiveCount, "Öncelikli müdahale gereken", "alert", "metric-warning", { activeOnly: "true", priority: "4" }],
            ["SLA Aşılan", dashboard.slaBreachedCount, "Hedef süresi geçen", "clock", "metric-danger", { slaStatus: "Breached" }],
            ["Bekleyen", dashboard.waitingCount, "Dış aksiyon bekleyen", "clock", "metric-warning", { status: "4" }],
            ["Çözülen", dashboard.resolvedCount, "Kapatılmayı bekleyen", "check", "metric-success", { status: "5" }],
            ["Süresi Yaklaşan", dashboard.slaDueSoonCount, "SLA hedefinin son bölümünde", "alert", "metric-warning", { slaStatus: "DueSoon" }],
            ["Toplam", dashboard.totalCount, "Kapsamınızdaki tüm talepler", "chart", "metric-neutral", {}],
        ] as const;

    return (
        <section aria-labelledby="dashboard-summary-title">
            <h2 id="dashboard-summary-title" className="visually-hidden">
                Talep özeti
            </h2>
            <div className="summary-grid">
                {cards.map(([label, value, description, icon, tone, query]) => (
                    <button
                        type="button"
                        className={`summary-card dashboard-metric-card ${tone}`}
                        key={`${label}-${description}`}
                        aria-label={`${label}: ${value}. İlgili talepleri aç.`}
                        onClick={() => onDrillDown(
                            query as Record<string, string>
                        )}
                    >
                        <div className="metric-card-header">
                            <span className="summary-label">{label}</span>
                            <span className="metric-icon">
                                <Icon name={icon as IconName} size={17} />
                            </span>
                        </div>
                        <strong className="summary-value">{value}</strong>
                        <span className="summary-description">{description}</span>
                    </button>
                ))}
            </div>
        </section>
    );
}

interface AdminOverviewProps {
    dashboard: NonNullable<DashboardDto["admin"]>;
    onDrillDown: (query: Record<string, string>) => void;
}

function AdminOverview({ dashboard, onDrillDown }: AdminOverviewProps) {
    const maximumWorkload = Math.max(
        1,
        ...dashboard.technicianWorkload.map((item) => item.activeTicketCount)
    );

    return (
        <section className="dashboard-admin-grid" aria-label="Yönetici özeti">
            <button
                type="button"
                className="card attention-card dashboard-attention-card"
                aria-label={`Atama bekleyen ${dashboard.unassignedOpenCount} talebi aç.`}
                onClick={() => onDrillDown({
                    status: "1",
                    unassignedOnly: "true",
                })}
            >
                <span className="summary-label">Atama Bekleyen Talepler</span>
                <strong className="summary-value">
                    {dashboard.unassignedOpenCount}
                </strong>
                <p className="page-description">
                    Açık durumda ve henüz teknik personele atanmamış talepler.
                </p>
            </button>

            <article className="card" aria-labelledby="workload-title">
                <div className="card-header">
                    <div>
                        <h2 id="workload-title">Teknik Personel İş Yükü</h2>
                        <p className="page-description">
                            Assigned, InProgress ve Waiting durumundaki talepler.
                        </p>
                    </div>
                </div>

                {dashboard.technicianWorkload.length === 0 ? (
                    <p className="empty-state">
                        Aktif teknik personel bulunmuyor.
                    </p>
                ) : (
                    <ul className="workload-list">
                        {dashboard.technicianWorkload.map((technician) => (
                            <li key={technician.technicianId}>
                                <span>{technician.fullName}</span>
                                <span className="workload-track" aria-hidden="true">
                                    <span style={{
                                        width: `${technician.activeTicketCount / maximumWorkload * 100}%`,
                                    }} />
                                </span>
                                <strong>
                                    {technician.activeTicketCount} aktif talep
                                </strong>
                            </li>
                        ))}
                    </ul>
                )}
            </article>
        </section>
    );
}

interface RecentTicketsProps {
    tickets: DashboardDto["recentTickets"];
}

function RecentTickets({ tickets }: RecentTicketsProps) {
    return (
        <section className="card" aria-labelledby="recent-tickets-title">
            <div className="card-header">
                <div>
                    <h2 id="recent-tickets-title">Son Talepler</h2>
                    <p className="page-description">
                        Erişim kapsamınızdaki en son beş talep.
                    </p>
                </div>
                <Link className="button button-secondary button-small" to="/tickets">
                    Tüm Talepler
                </Link>
            </div>

            {tickets.length === 0 ? (
                <p className="empty-state">Henüz görüntülenecek talep bulunmuyor.</p>
            ) : (
                <div className="table-container dashboard-table-container">
                    <table>
                        <thead>
                            <tr>
                                <th>Talep</th>
                                <th>Durum</th>
                                <th>Öncelik</th>
                                <th>Teknik Personel</th>
                                <th>Oluşturulma</th>
                            </tr>
                        </thead>
                        <tbody>
                            {tickets.map((ticket) => (
                                <tr key={ticket.id}>
                                    <td className="ticket-primary-cell">
                                        <Link
                                            className="table-link ticket-number"
                                            to={`/tickets/${ticket.id}`}
                                        >
                                            {ticket.ticketNumber}
                                        </Link>
                                        <span className="ticket-cell-secondary">
                                            {ticket.title} · {ticket.assetName}
                                        </span>
                                    </td>
                                    <td><TicketStatusBadge status={ticket.status} /></td>
                                    <td><TicketPriorityBadge priority={ticket.priority} /></td>
                                    <td>
                                        {ticket.assignedTechnicianFullName ?? (
                                            <span className="muted-text">Atanmadı</span>
                                        )}
                                    </td>
                                    <td>{formatDate(ticket.createdAt)}</td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
        </section>
    );
}

function formatDate(value: string): string {
    return new Date(value).toLocaleString("tr-TR");
}

function navigateToTickets(
    navigate: ReturnType<typeof useNavigate>,
    query: Record<string, string>
) {
    const searchParams = new URLSearchParams(query);
    navigate(searchParams.size > 0
        ? `/tickets?${searchParams.toString()}`
        : "/tickets");
}
