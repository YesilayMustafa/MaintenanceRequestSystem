import type { SlaStatus } from "../types/tickets";

export function formatSlaRemainingTime(
    status: SlaStatus,
    remainingMinutes: number | null
): string | null {
    if (
        remainingMinutes === null ||
        status === "Met" ||
        status === "NotApplicable"
    ) {
        return null;
    }

    const isLate = remainingMinutes < 0;
    const absoluteMinutes = Math.abs(remainingMinutes);
    const days = Math.floor(absoluteMinutes / 1440);
    const hours = Math.floor((absoluteMinutes % 1440) / 60);
    const minutes = absoluteMinutes % 60;
    const parts: string[] = [];

    if (days > 0) {
        parts.push(`${days} gün`);
    }

    if (hours > 0) {
        parts.push(`${hours} sa`);
    }

    if (minutes > 0 || parts.length === 0) {
        parts.push(`${minutes} dk`);
    }

    return `${parts.join(" ")} ${isLate ? "gecikti" : "kaldı"}`;
}
