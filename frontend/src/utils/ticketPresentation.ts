import type { TicketPriority, TicketStatus } from "../types/tickets";

const statusLabels: Record<TicketStatus, string> = {
    Open: "Açık",
    Assigned: "Atandı",
    InProgress: "İşlemde",
    Waiting: "Bekliyor",
    Resolved: "Çözüldü",
    Closed: "Kapandı",
    Cancelled: "İptal",
};

const priorityLabels: Record<TicketPriority, string> = {
    Low: "Düşük",
    Medium: "Orta",
    High: "Yüksek",
    Critical: "Kritik",
};

export function getTicketStatusLabel(status: TicketStatus | string): string {
    return statusLabels[status as TicketStatus] ?? status;
}

export function getTicketPriorityLabel(priority: TicketPriority): string {
    return priorityLabels[priority];
}
