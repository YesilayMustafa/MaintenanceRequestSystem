import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";

import { getAssetMaintenanceHistory } from "../api/assetMaintenanceHistoryApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import { TicketPriorityBadge, TicketStatusBadge } from "../components/TicketBadges";

import type { AssetMaintenanceHistoryDto } from "../types/assetMaintenanceHistory";
import type { AssetTypeName } from "../types/assets";

const pageSize = 10;
const assetTypeLabels: Record<AssetTypeName, string> = {
    Computer: "Bilgisayar", Printer: "Yazıcı", Server: "Sunucu",
    NetworkDevice: "Ağ Cihazı", SoftwareSystem: "Yazılım Sistemi", Other: "Diğer",
};

export function AssetMaintenanceHistoryPage() {
    const { id } = useParams();
    const { token } = useAuth();
    const [history, setHistory] = useState<AssetMaintenanceHistoryDto | null>(null);
    const [pageNumber, setPageNumber] = useState(1);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;
        async function load() {
            if (!token || !id) return;
            try {
                setIsLoading(true);
                setError(null);
                const result = await getAssetMaintenanceHistory(token, id, pageNumber, pageSize);
                if (!cancelled) setHistory(result);
            } catch (error) {
                if (!cancelled) setError(getErrorMessage(error, "Bakım geçmişi yüklenemedi."));
            } finally {
                if (!cancelled) setIsLoading(false);
            }
        }
        load();
        return () => { cancelled = true; };
    }, [id, pageNumber, token]);

    if (isLoading && !history) return <p className="loading-state">Bakım geçmişi yükleniyor...</p>;
    if (error) return <p className="error-state" role="alert">{error}</p>;
    if (!history) return <p className="empty-state">Cihaz bulunamadı.</p>;

    const summaryCards = [
        ["Toplam Talep", history.summary.totalTicketCount],
        ["Aktif", history.summary.activeTicketCount],
        ["Çözülen", history.summary.resolvedTicketCount],
        ["Kapatılan", history.summary.closedTicketCount],
        ["Kritik", history.summary.criticalTicketCount],
        ["Son Talep", history.summary.lastTicketCreatedAt ? formatDate(history.summary.lastTicketCreatedAt) : "—"],
    ];

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <Link to="/assets" className="table-link">← Cihazlara dön</Link>
                    <h1 className="page-title">Bakım Geçmişi</h1>
                    <p className="page-description">
                        {history.asset.name} · {history.asset.serialNumber} · {assetTypeLabels[history.asset.type]}
                    </p>
                </div>
            </header>

            <section className="summary-grid" aria-label="Bakım geçmişi özeti">
                {summaryCards.map(([label, value]) => (
                    <article className="summary-card" key={label}>
                        <span>{label}</span><strong>{value}</strong>
                    </article>
                ))}
            </section>

            <section aria-labelledby="maintenance-records-title">
                <div className="card-header">
                    <div><h2 id="maintenance-records-title">Talep Kayıtları</h2>
                        <p className="page-description">Yetkiniz kapsamındaki bakım ve arıza talepleri.</p>
                    </div>
                </div>
                {history.tickets.items.length === 0 ? (
                    <p className="empty-state">Bu cihaz için görüntüleyebileceğiniz bakım kaydı bulunmuyor.</p>
                ) : (
                    <div className="table-container maintenance-history-table">
                        <table><thead><tr>
                            <th>Talep</th><th>Kategori</th><th>Durum</th><th>Öncelik</th>
                            <th>Oluşturan</th><th>Teknik Personel</th><th>Oluşturulma</th><th>Çözülme / Kapanma</th>
                        </tr></thead><tbody>
                            {history.tickets.items.map((ticket) => (
                                <tr key={ticket.id}>
                                    <td><Link to={`/tickets/${ticket.id}`} className="table-link">{ticket.ticketNumber}</Link>
                                        <span className="maintenance-ticket-title">{ticket.title}</span></td>
                                    <td>{ticket.categoryName}</td>
                                    <td><TicketStatusBadge status={ticket.status} /></td>
                                    <td><TicketPriorityBadge priority={ticket.priority} /></td>
                                    <td>{ticket.createdByFullName}</td>
                                    <td>{ticket.assignedTechnicianFullName ?? "Atanmadı"}</td>
                                    <td>{formatDate(ticket.createdAt)}</td>
                                    <td>{ticket.closedAt ? `Kapandı: ${formatDate(ticket.closedAt)}` : ticket.resolvedAt ? `Çözüldü: ${formatDate(ticket.resolvedAt)}` : "—"}</td>
                                </tr>
                            ))}
                        </tbody></table>
                    </div>
                )}
            </section>

            {history.tickets.totalPages > 1 && (
                <nav className="pagination" aria-label="Bakım geçmişi sayfaları">
                    <p className="pagination-summary">Sayfa {history.tickets.pageNumber} / {history.tickets.totalPages}</p>
                    <div className="pagination-actions">
                        <button type="button" disabled={pageNumber <= 1 || isLoading} onClick={() => setPageNumber((page) => page - 1)}>Önceki</button>
                        <button type="button" disabled={pageNumber >= history.tickets.totalPages || isLoading} onClick={() => setPageNumber((page) => page + 1)}>Sonraki</button>
                    </div>
                </nav>
            )}
        </div>
    );
}

function formatDate(value: string): string { return new Date(value).toLocaleString("tr-TR"); }
function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof ApiError ? error.message : fallback;
}
