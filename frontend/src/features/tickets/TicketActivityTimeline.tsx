import { useEffect, useState } from "react";

import { ApiError } from "../../api/httpClient";
import { getTicketActivity } from "../../api/ticketActivityApi";

import type { TicketActivityDto } from "../../types/ticketActivity";

const pageSize = 20;
const markerClasses: Record<string, string> = {
    TicketCreated: "activity-created",
    AssignmentChanged: "activity-assignment",
    StatusChanged: "activity-status",
    PriorityChanged: "activity-priority",
    CategoryChanged: "activity-category",
    CommentAdded: "activity-comment",
    AttachmentUploaded: "activity-attachment",
};

interface TicketActivityTimelineProps {
    ticketId: string;
    token: string;
    refreshKey: number;
}

export function TicketActivityTimeline({
    ticketId,
    token,
    refreshKey,
}: TicketActivityTimelineProps) {
    const [items, setItems] = useState<TicketActivityDto[]>([]);
    const [pageNumber, setPageNumber] = useState(1);
    const [totalPages, setTotalPages] = useState(0);
    const [isLoading, setIsLoading] = useState(true);
    const [isLoadingMore, setIsLoadingMore] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadFirstPage() {
            try {
                setIsLoading(true);
                setError(null);
                const result = await getTicketActivity(
                    token,
                    ticketId,
                    1,
                    pageSize
                );

                if (!cancelled) {
                    setItems(result.items);
                    setPageNumber(1);
                    setTotalPages(result.totalPages);
                }
            } catch (caughtError) {
                if (!cancelled) {
                    setError(getErrorMessage(caughtError));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadFirstPage();
        return () => {
            cancelled = true;
        };
    }, [refreshKey, ticketId, token]);

    async function loadMore() {
        if (isLoadingMore || pageNumber >= totalPages) {
            return;
        }

        try {
            setIsLoadingMore(true);
            setError(null);
            const nextPage = pageNumber + 1;
            const result = await getTicketActivity(
                token,
                ticketId,
                nextPage,
                pageSize
            );

            setItems((currentItems) => {
                const knownIds = new Set(currentItems.map((item) => item.id));
                return [
                    ...currentItems,
                    ...result.items.filter((item) => !knownIds.has(item.id)),
                ];
            });
            setPageNumber(nextPage);
            setTotalPages(result.totalPages);
        } catch (caughtError) {
            setError(getErrorMessage(caughtError));
        } finally {
            setIsLoadingMore(false);
        }
    }

    return (
        <section className="card ticket-activity-card" aria-labelledby="activity-title">
            <div className="card-header">
                <div>
                    <h2 id="activity-title">Talep Aktivitesi</h2>
                    <p className="page-description">
                        Talep üzerindeki işlemlerin birleşik zaman akışı.
                    </p>
                </div>
            </div>

            {isLoading ? (
                <p className="loading-state" role="status">Aktivite yükleniyor...</p>
            ) : items.length === 0 ? (
                <p className="empty-state">
                    Bu talep için henüz aktivite kaydı bulunmuyor.
                </p>
            ) : (
                <ol className="activity-timeline">
                    {items.map((item) => (
                        <li className="activity-item" key={item.id}>
                            <span
                                className={`activity-marker ${markerClasses[item.type] ?? "activity-generic"}`}
                                aria-hidden="true"
                            />
                            <div className="activity-content">
                                <h3>{item.title || "Talep aktivitesi"}</h3>
                                {item.description && <p>{item.description}</p>}
                                <div className="activity-meta">
                                    <span>{item.actorFullName}</span>
                                    <time dateTime={item.createdAt}>
                                        {new Date(item.createdAt).toLocaleString("tr-TR")}
                                    </time>
                                </div>
                            </div>
                        </li>
                    ))}
                </ol>
            )}

            {error && <p className="error-state" role="alert">{error}</p>}

            {pageNumber < totalPages && (
                <div className="activity-load-more">
                    <button
                        type="button"
                        className="button button-secondary"
                        disabled={isLoadingMore}
                        onClick={loadMore}
                    >
                        {isLoadingMore ? "Yükleniyor..." : "Daha Fazla Göster"}
                    </button>
                </div>
            )}
        </section>
    );
}

function getErrorMessage(error: unknown): string {
    return error instanceof ApiError
        ? error.message
        : "Talep aktivitesi yüklenemedi.";
}
