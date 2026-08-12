import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";
import {
    Link,
    useNavigate,
    useParams,
} from "react-router-dom";

import {
    createTicketComment,
    getTicketComments,
} from "../api/commentsApi";
import { ApiError } from "../api/httpClient";
import {
    getTicketById,
    getTicketHistory,
} from "../api/ticketsApi";
import { useAuth } from "../auth/useAuth";
import {
    TicketPriorityBadge,
    TicketStatusBadge,
} from "../components/TicketBadges";
import { TicketActions } from "../features/tickets/TicketActions";
import { TicketAttachments } from "../features/tickets/TicketAttachments";

import type { TicketCommentDto } from "../types/comments";
import type {
    TicketDto,
    TicketHistoryDto,
} from "../types/tickets";

export function TicketDetailsPage() {
    const { id } = useParams();
    const navigate = useNavigate();
    const { user, token } = useAuth();

    const [ticket, setTicket] = useState<TicketDto | null>(null);
    const [history, setHistory] = useState<TicketHistoryDto[]>([]);
    const [comments, setComments] = useState<TicketCommentDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [commentContent, setCommentContent] = useState("");
    const [isCommentSubmitting, setIsCommentSubmitting] = useState(false);
    const [commentError, setCommentError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadTicketDetails() {
            if (!token || !id) {
                return;
            }

            try {
                setIsLoading(true);
                setError(null);

                const [ticketResult, historyResult, commentsResult] =
                    await Promise.all([
                        getTicketById(token, id),
                        getTicketHistory(token, id),
                        getTicketComments(token, id),
                    ]);

                if (!cancelled) {
                    setTicket(ticketResult);
                    setHistory(historyResult);
                    setComments(commentsResult);
                }
            } catch (error) {
                if (cancelled) {
                    return;
                }

                if (error instanceof ApiError) {
                    setError(error.message);
                } else {
                    setError("Ticket detayı yüklenirken beklenmeyen bir hata oluştu.");
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadTicketDetails();

        return () => {
            cancelled = true;
        };
    }, [token, id]);

    async function handleCommentSubmit(
        event: SubmitEvent<HTMLFormElement>
    ) {
        event.preventDefault();

        const content = commentContent.trim();

        if (!token || !id || !content) {
            return;
        }

        try {
            setIsCommentSubmitting(true);
            setCommentError(null);

            const createdComment =
                await createTicketComment(
                    token,
                    id,
                    { content }
                );

            setComments((currentComments) => [
                ...currentComments,
                createdComment,
            ]);

            setCommentContent("");
        } catch (error) {
            if (error instanceof ApiError) {
                setCommentError(error.message);
            } else {
                setCommentError(
                    "Yorum eklenirken beklenmeyen bir hata oluştu."
                );
            }
        } finally {
            setIsCommentSubmitting(false);
        }
    }

    async function handleTicketUpdated(
        updatedTicket: TicketDto
    ) {
        if (!token || !id) {
            return;
        }

        setTicket(updatedTicket);

        const updatedHistory =
            await getTicketHistory(token, id);

        setHistory(updatedHistory);
    }

    if (isLoading) {
        return <p className="loading-state">Ticket yükleniyor...</p>;
    }

    if (error) {
        return <p className="error-state" role="alert">{error}</p>;
    }

    if (!ticket) {
        return <p className="empty-state">Ticket bulunamadı.</p>;
    }

    return (
        <div className="page">
            <header className="page-header">
                <div>
                    <Link to="/tickets" className="table-link">
                        ← Talep listesine dön
                    </Link>
                    <p className="ticket-reference">{ticket.ticketNumber}</p>
                    <h1 className="page-title">{ticket.title}</h1>
                    <div className="page-header-actions">
                        <TicketStatusBadge status={ticket.status} />
                        <TicketPriorityBadge priority={ticket.priority} />
                    </div>
                </div>
            </header>

            <div className="details-grid">
                <div className="details-stack">
                    <section className="card" aria-labelledby="ticket-info-title">
                        <div className="card-header">
                            <h2 id="ticket-info-title">Talep Bilgileri</h2>
                        </div>

                        <h3>Açıklama</h3>
                        <p className="description-text">{ticket.description}</p>

                        <dl className="definition-grid">
                            <div className="definition-item">
                                <dt>Talep No</dt>
                                <dd className="ticket-number">{ticket.ticketNumber}</dd>
                            </div>
                            <div className="definition-item">
                                <dt>Cihaz</dt>
                                <dd>{ticket.assetName}</dd>
                            </div>
                            <div className="definition-item">
                                <dt>Kategori</dt>
                                <dd>{ticket.categoryName}</dd>
                            </div>
                            <div className="definition-item">
                                <dt>Seri Numarası</dt>
                                <dd>{ticket.assetSerialNumber}</dd>
                            </div>
                            <div className="definition-item">
                                <dt>Oluşturan</dt>
                                <dd>{ticket.createdByFullName}</dd>
                            </div>
                            <div className="definition-item">
                                <dt>Atanan Teknik Personel</dt>
                                <dd>
                                    {ticket.assignedTechnicianFullName ?? (
                                        <span className="muted-text">Atanmadı</span>
                                    )}
                                </dd>
                            </div>
                            <div className="definition-item">
                                <dt>Oluşturulma</dt>
                                <dd>
                                    {new Date(ticket.createdAt)
                                        .toLocaleString("tr-TR")}
                                </dd>
                            </div>
                            {ticket.updatedAt && (
                                <div className="definition-item">
                                    <dt>Son Güncelleme</dt>
                                    <dd>
                                        {new Date(ticket.updatedAt)
                                            .toLocaleString("tr-TR")}
                                    </dd>
                                </div>
                            )}
                            {ticket.waitingReason && (
                                <div className="definition-item form-group-full">
                                    <dt>Bekleme Nedeni</dt>
                                    <dd>{ticket.waitingReason}</dd>
                                </div>
                            )}
                            {ticket.resolutionDescription && (
                                <div className="definition-item form-group-full">
                                    <dt>Çözüm Açıklaması</dt>
                                    <dd>{ticket.resolutionDescription}</dd>
                                </div>
                            )}
                        </dl>
                    </section>

                    {user && token && (
                        <TicketAttachments
                            ticketId={ticket.id}
                            ticketStatus={ticket.status}
                            token={token}
                            user={user}
                        />
                    )}

                    <section className="card" aria-labelledby="history-title">
                        <div className="card-header">
                            <h2 id="history-title">Talep Geçmişi</h2>
                        </div>

                {history.length === 0 ? (
                            <p className="empty-state">Geçmiş kaydı bulunamadı.</p>
                ) : (
                            <ol className="timeline">
                        {history.map((historyItem) => (
                                    <li
                                        className="timeline-item"
                                        key={historyItem.id}
                                    >
                                        <p className="timeline-title">
                                    <strong>
                                        {historyItem.oldStatus ?? "Başlangıç"}
                                                {" → "}
                                        {historyItem.newStatus}
                                    </strong>
                                </p>

                                        <p className="timeline-description">
                                            {historyItem.description}
                                        </p>

                                        <p className="timeline-date">
                                    {new Date(
                                        historyItem.occurredAt
                                    ).toLocaleString("tr-TR")}
                                </p>
                            </li>
                        ))}
                            </ol>
                )}
            </section>

                    <section className="card" aria-labelledby="comments-title">
                        <div className="card-header">
                            <h2 id="comments-title">Yorumlar</h2>
                        </div>

                {comments.length === 0 ? (
                            <p className="empty-state">Henüz yorum bulunmuyor.</p>
                ) : (
                            <ul className="comment-list">
                        {comments.map((comment) => (
                                    <li className="comment-item" key={comment.id}>
                                        <div className="comment-meta">
                                            <div>
                                                <strong className="comment-author">
                                                    {comment.userFullName}
                                                </strong>
                                                <span className="comment-role">
                                                    {comment.userRole}
                                                </span>
                                            </div>
                                            <p className="comment-date">
                                    {new Date(
                                        comment.createdAt
                                    ).toLocaleString("tr-TR")}
                                            </p>
                                        </div>

                                        <p className="comment-content">
                                            {comment.content}
                                        </p>
                            </li>
                        ))}
                    </ul>
                )}

                {ticket.status !== "Closed" &&
                    ticket.status !== "Cancelled" && (
                                <form
                                    className="comment-form"
                                    onSubmit={handleCommentSubmit}
                                >
                            <label htmlFor="comment-content">
                                Yeni Yorum
                            </label>

                            <textarea
                                id="comment-content"
                                value={commentContent}
                                onChange={(event) =>
                                    setCommentContent(event.target.value)
                                }
                                disabled={isCommentSubmitting}
                            />

                                    <div className="form-actions">
                                        <button
                                            type="submit"
                                            className="button button-primary"
                                            disabled={
                                                isCommentSubmitting ||
                                                !commentContent.trim()
                                            }
                                        >
                                            {isCommentSubmitting
                                                ? "Gönderiliyor..."
                                                : "Yorum Ekle"}
                                        </button>
                                    </div>

                            {commentError && (
                                        <p className="error-state" role="alert">
                                            {commentError}
                                        </p>
                            )}
                        </form>
                    )}
            </section>
                </div>

                <aside>
                    {user && token && (
                        <TicketActions
                            ticket={ticket}
                            user={user}
                            token={token}
                            onTicketUpdated={handleTicketUpdated}
                            onSoftDeleted={() => navigate("/tickets")}
                        />
                    )}
                </aside>
            </div>
        </div>
    );
}
