import type { CSSProperties } from "react";

import type { SlaStatus, TicketPriority } from "../types/tickets";
import type { TicketTimelineItemDto } from "../types/timeline";

export function getTimelineBarStyle(
    item: TicketTimelineItemDto,
    rangeStartDate: Date,
    rangeEndDate: Date
): CSSProperties | null {
    const rangeStart = rangeStartDate.getTime();
    const rangeEnd = rangeEndDate.getTime();
    const ticketStart = new Date(item.createdAt).getTime();
    const ticketEnd = new Date(item.slaDueAt).getTime();

    if (
        !Number.isFinite(ticketStart) ||
        !Number.isFinite(ticketEnd) ||
        rangeEnd <= rangeStart ||
        ticketEnd < ticketStart ||
        ticketEnd < rangeStart ||
        ticketStart > rangeEnd
    ) {
        return null;
    }

    const visibleStart = Math.max(ticketStart, rangeStart);
    const visibleEnd = Math.min(ticketEnd, rangeEnd);
    const duration = rangeEnd - rangeStart;
    const naturalLeft = (visibleStart - rangeStart) / duration * 100;
    const width = Math.min(
        100,
        Math.max(1.5, (visibleEnd - visibleStart) / duration * 100)
    );

    return {
        left: `${Math.min(naturalLeft, 100 - width)}%`,
        width: `${width}%`,
    };
}

export function getTimelinePriorityClass(priority: TicketPriority): string {
    return `timeline-priority-${priority.toLowerCase()}`;
}

export function getTimelineBarClass(
    baseClass: string,
    item: TicketTimelineItemDto
): string {
    const breachedClass = item.slaStatus === "Breached"
        ? " timeline-bar-breached"
        : "";

    return `${baseClass} ${getTimelinePriorityClass(item.priority)}${breachedClass}`;
}

export function getTimelineTooltip(item: TicketTimelineItemDto): string {
    return [
        `${item.ticketNumber} · ${item.title}`,
        `Başlangıç: ${formatTimelineDate(item.createdAt)}`,
        `SLA son: ${formatTimelineDate(item.slaDueAt)}`,
        `Durum: ${item.status}`,
        `Öncelik: ${item.priority}`,
        `SLA: ${item.slaStatus}`,
    ].join("\n");
}

export function formatSlaTimeState(
    slaDueAt: string,
    slaStatus: SlaStatus,
    now = new Date()
): string {
    if (slaStatus === "Met") {
        return "SLA karşılandı";
    }

    if (slaStatus === "NotApplicable") {
        return "Uygulanamaz";
    }

    const dueAt = new Date(slaDueAt);

    if (!Number.isFinite(dueAt.getTime())) {
        return "SLA süresi bilinmiyor";
    }

    const differenceMilliseconds = slaStatus === "Breached"
        ? now.getTime() - dueAt.getTime()
        : dueAt.getTime() - now.getTime();
    const duration = formatCompactDuration(
        Math.max(0, differenceMilliseconds)
    );

    return slaStatus === "Breached"
        ? `${duration} geçti`
        : `${duration} kaldı`;
}

export function getSlaTimeStateClass(slaStatus: SlaStatus): string {
    switch (slaStatus) {
        case "DueSoon":
            return "timeline-sla-state-warning";
        case "Breached":
            return "timeline-sla-state-danger";
        case "Met":
            return "timeline-sla-state-success";
        default:
            return "timeline-sla-state-muted";
    }
}

function formatTimelineDate(value: string): string {
    return new Date(value).toLocaleString("tr-TR", {
        dateStyle: "short",
        timeStyle: "short",
    });
}

function formatCompactDuration(milliseconds: number): string {
    const totalMinutes = Math.floor(milliseconds / 60_000);

    if (totalMinutes < 60) {
        return `${totalMinutes} dk`;
    }

    const totalHours = Math.floor(totalMinutes / 60);
    const days = Math.floor(totalHours / 24);
    const hours = totalHours % 24;

    if (days > 0) {
        return hours > 0
            ? `${days} gün ${hours} saat`
            : `${days} gün`;
    }

    return `${totalHours} saat`;
}
