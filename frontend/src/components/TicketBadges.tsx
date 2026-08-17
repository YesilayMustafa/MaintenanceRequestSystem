import type {
    TicketPriority,
    TicketStatus,
} from "../types/tickets";
import {
    getTicketPriorityLabel,
    getTicketStatusLabel,
} from "../utils/ticketPresentation";

interface TicketStatusBadgeProps {
    status: TicketStatus;
}

export function TicketStatusBadge({ status }: TicketStatusBadgeProps) {
    return (
        <span className={`badge badge-status badge-${status.toLowerCase()}`}>
            {getTicketStatusLabel(status)}
        </span>
    );
}

interface TicketPriorityBadgeProps {
    priority: TicketPriority;
}

export function TicketPriorityBadge({
    priority,
}: TicketPriorityBadgeProps) {
    return (
        <span className={`badge badge-priority badge-${priority.toLowerCase()}`}>
            {getTicketPriorityLabel(priority)}
        </span>
    );
}
