import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";

import { getTicketTimeline } from "../api/timelineApi";
import {
    formatSlaTimeState,
    getSlaTimeStateClass,
    getTimelineBarClass,
    getTimelineBarStyle,
    getTimelineTooltip,
} from "../utils/timelinePresentation";
import { getWeekRange } from "../utils/weekRange";

import type { TicketTimelineItemDto } from "../types/timeline";

export function WeeklyTimelinePreview({ token }: { token: string }) {
    const week = useMemo(() => getWeekRange(), []);
    const [items, setItems] = useState<TicketTimelineItemDto[]>([]);

    useEffect(() => {
        let cancelled = false;

        getTicketTimeline(token, {
            from: week.start.toISOString(),
            to: week.end.toISOString(),
        })
            .then((result) => {
                if (!cancelled) setItems(result.slice(0, 5));
            })
            .catch(() => {
                if (!cancelled) setItems([]);
            });

        return () => { cancelled = true; };
    }, [token, week]);

    return (
        <section className="card dashboard-timeline-preview" aria-labelledby="dashboard-timeline-title">
            <div className="card-header">
                <div>
                    <h2 id="dashboard-timeline-title">Bu Haftanın Talep Akışı</h2>
                    <p className="page-description">Erişim kapsamınızdaki ilk beş talep.</p>
                </div>
                <Link className="button button-secondary button-small" to="/timeline">
                    Tümünü Gör
                </Link>
            </div>
            {items.length === 0 ? (
                <p className="empty-state">Bu hafta için talep akışı bulunmuyor.</p>
            ) : (
                <ul className="timeline-preview-list">
                    {items.map((item) => (
                        <li key={item.id}>
                            <Link to={`/tickets/${item.id}`}>{item.ticketNumber}</Link>
                            <span className="timeline-preview-track">
                                <TimelinePreviewBar item={item} rangeStart={week.start} rangeEnd={week.end} />
                            </span>
                            <span className={`timeline-preview-sla-state ${getSlaTimeStateClass(item.slaStatus)}`}>
                                {formatSlaTimeState(item.slaDueAt, item.slaStatus)}
                            </span>
                            <small>{item.title}</small>
                        </li>
                    ))}
                </ul>
            )}
        </section>
    );
}

function TimelinePreviewBar({
    item,
    rangeStart,
    rangeEnd,
}: {
    item: TicketTimelineItemDto;
    rangeStart: Date;
    rangeEnd: Date;
}) {
    const style = getTimelineBarStyle(item, rangeStart, rangeEnd);

    if (!style) return null;

    return (
        <span
            className={getTimelineBarClass("timeline-preview-bar", item)}
            style={style}
            title={getTimelineTooltip(item)}
        />
    );
}
