import { useEffect, useState } from "react";
import { Link } from "react-router-dom";

import { getDashboard } from "../api/dashboardApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import {
    TicketPriorityBadge,
    TicketStatusBadge,
} from "../components/TicketBadges";

import type { DashboardDto } from "../types/dashboard";
import type { UserRole } from "../types/auth";

const roleDescriptions: Record<UserRole, string> = {
    Employee: "Taleplerinizin güncel durumunu takip edin.",
    Technician: "Size atanan teknik taleplerin durumunu takip edin.",
    Admin: "Sistem genelindeki bakım ve arıza süreçlerini takip edin.",
};

export function DashboardPage() {
    const { token, user } = useAuth();
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

                {user?.role === "Employee" && (
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
                    <SummaryCards dashboard={dashboard} role={user?.role} />

                    {dashboard.admin && (
                        <AdminOverview dashboard={dashboard.admin} />
                    )}

                    <RecentTickets tickets={dashboard.recentTickets} />
                </>
            )}
        </div>
    );
}

interface SummaryCardsProps {
    dashboard: DashboardDto;
    role: UserRole | undefined;
}

function SummaryCards({ dashboard, role }: SummaryCardsProps) {
    const cards = role === "Technician"
        ? [
            ["Toplam Atanan", dashboard.totalCount, "Kapsamınızdaki tüm talepler"],
            ["Atanan", dashboard.assignedCount, "Henüz işleme alınmayan"],
            ["İşlemde", dashboard.inProgressCount, "Üzerinde çalışılan"],
            ["Bekleyen", dashboard.waitingCount, "Dış aksiyon bekleyen"],
            ["Çözülen", dashboard.resolvedCount, "Çözümü tamamlanan"],
            ["Kritik Aktif", dashboard.criticalActiveCount, "Öncelikli müdahale gereken"],
            ["SLA Aşılan", dashboard.slaBreachedCount, "Hedef süresi geçen"],
            ["Süresi Yaklaşan", dashboard.slaDueSoonCount, "SLA hedefinin son bölümünde"],
        ] as const
        : [
            ["Toplam", dashboard.totalCount, "Kapsamınızdaki tüm talepler"],
            ["Aktif Süreç", dashboard.activeCount, "Open–Resolved arası süreçler"],
            ["İşlemde", dashboard.inProgressCount, "Üzerinde çalışılan"],
            ["Bekleyen", dashboard.waitingCount, "Dış aksiyon bekleyen"],
            ["Çözülen", dashboard.resolvedCount, "Kapatılmayı bekleyen"],
            ["Kritik Aktif", dashboard.criticalActiveCount, "Öncelikli müdahale gereken"],
            ["SLA Aşılan", dashboard.slaBreachedCount, "Hedef süresi geçen"],
            ["Süresi Yaklaşan", dashboard.slaDueSoonCount, "SLA hedefinin son bölümünde"],
        ] as const;

    return (
        <section aria-labelledby="dashboard-summary-title">
            <h2 id="dashboard-summary-title" className="visually-hidden">
                Talep özeti
            </h2>
            <div className="summary-grid">
                {cards.map(([label, value, description]) => (
                    <article className="summary-card" key={label}>
                        <span className="summary-label">{label}</span>
                        <strong className="summary-value">{value}</strong>
                        <span className="summary-description">{description}</span>
                    </article>
                ))}
            </div>
        </section>
    );
}

interface AdminOverviewProps {
    dashboard: NonNullable<DashboardDto["admin"]>;
}

function AdminOverview({ dashboard }: AdminOverviewProps) {
    return (
        <section className="dashboard-admin-grid" aria-label="Yönetici özeti">
            <article className="card attention-card">
                <span className="summary-label">Atama Bekleyen Talepler</span>
                <strong className="summary-value">
                    {dashboard.unassignedOpenCount}
                </strong>
                <p className="page-description">
                    Açık durumda ve henüz teknik personele atanmamış talepler.
                </p>
            </article>

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
                                <th>Talep No</th>
                                <th>Başlık</th>
                                <th>Durum</th>
                                <th>Öncelik</th>
                                <th>Cihaz</th>
                                <th>Teknik Personel</th>
                                <th>Oluşturulma</th>
                            </tr>
                        </thead>
                        <tbody>
                            {tickets.map((ticket) => (
                                <tr key={ticket.id}>
                                    <td>
                                        <Link
                                            className="table-link ticket-number"
                                            to={`/tickets/${ticket.id}`}
                                        >
                                            {ticket.ticketNumber}
                                        </Link>
                                    </td>
                                    <td>
                                        <Link
                                            className="table-link"
                                            to={`/tickets/${ticket.id}`}
                                        >
                                            {ticket.title}
                                        </Link>
                                    </td>
                                    <td><TicketStatusBadge status={ticket.status} /></td>
                                    <td><TicketPriorityBadge priority={ticket.priority} /></td>
                                    <td>{ticket.assetName}</td>
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
