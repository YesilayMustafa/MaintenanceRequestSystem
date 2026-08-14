import type { SlaStatus } from "../types/tickets";

const slaLabels: Record<SlaStatus, string> = {
    OnTrack: "Süre İçinde",
    DueSoon: "Süre Yaklaşıyor",
    Breached: "SLA Aşıldı",
    Met: "SLA Karşılandı",
    NotApplicable: "Uygulanamaz",
};

interface SlaBadgeProps {
    status: SlaStatus;
}

export function SlaBadge({ status }: SlaBadgeProps) {
    return (
        <span className={`badge badge-sla badge-sla-${status.toLowerCase()}`}>
            {slaLabels[status]}
        </span>
    );
}
