import type {
    TicketPriority,
    TicketStatus,
} from "../types/tickets";

interface TicketStatusBadgeProps {
    status: TicketStatus;
}

export function TicketStatusBadge({ status }: TicketStatusBadgeProps) {
    return (
        <span className={`badge badge-status badge-${status.toLowerCase()}`}>
            {status}
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
            {priority}
        </span>
    );
}
