import {
    useEffect,
    useState,
    type SubmitEvent,
} from "react";
import {
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
import { TicketActions } from "../features/tickets/TicketActions";

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
        return <p>Ticket yükleniyor...</p>;
    }

    if (error) {
        return <p role="alert">{error}</p>;
    }

    if (!ticket) {
        return <p>Ticket bulunamadı.</p>;
    }

    return (
        <main>
            <h1>{ticket.title}</h1>

            <p><strong>Durum:</strong> {ticket.status}</p>
            <p><strong>Öncelik:</strong> {ticket.priority}</p>
            <p><strong>Açıklama:</strong> {ticket.description}</p>

            <p>
                <strong>Cihaz:</strong>{" "}
                {ticket.assetName} ({ticket.assetSerialNumber})
            </p>

            <p>
                <strong>Oluşturan:</strong>{" "}
                {ticket.createdByFullName}
            </p>

            <p>
                <strong>Teknisyen:</strong>{" "}
                {ticket.assignedTechnicianFullName ?? "Atanmadı"}
            </p>

            <p>
                <strong>Oluşturulma:</strong>{" "}
                {new Date(ticket.createdAt).toLocaleString("tr-TR")}
            </p>

            {ticket.waitingReason && (
                <p>
                    <strong>Bekleme Nedeni:</strong>{" "}
                    {ticket.waitingReason}
                </p>
            )}

            {ticket.resolutionDescription && (
                <p>
                    <strong>Çözüm Açıklaması:</strong>{" "}
                    {ticket.resolutionDescription}
                </p>
            )}

            {user && token && (
                <TicketActions
                    ticket={ticket}
                    user={user}
                    token={token}
                    onTicketUpdated={handleTicketUpdated}
                    onSoftDeleted={() => navigate("/tickets")}
                />
            )}

            <section>
                <h2>Ticket Geçmişi</h2>

                {history.length === 0 ? (
                    <p>Geçmiş kaydı bulunamadı.</p>
                ) : (
                    <ul>
                        {history.map((historyItem) => (
                            <li key={historyItem.id}>
                                <p>
                                    <strong>
                                        {historyItem.oldStatus ?? "Başlangıç"}
                                        {" → "}
                                        {historyItem.newStatus}
                                    </strong>
                                </p>

                                <p>{historyItem.description}</p>

                                <p>
                                    {new Date(
                                        historyItem.occurredAt
                                    ).toLocaleString("tr-TR")}
                                </p>
                            </li>
                        ))}
                    </ul>
                )}
            </section>

            <section>
                <h2>Yorumlar</h2>

                {comments.length === 0 ? (
                    <p>Henüz yorum bulunmuyor.</p>
                ) : (
                    <ul>
                        {comments.map((comment) => (
                            <li key={comment.id}>
                                <p>
                                    <strong>{comment.userFullName}</strong>
                                    {" — "}
                                    {comment.userRole}
                                </p>

                                <p>{comment.content}</p>

                                <p>
                                    {new Date(
                                        comment.createdAt
                                    ).toLocaleString("tr-TR")}
                                </p>
                            </li>
                        ))}
                    </ul>
                )}

                {ticket.status !== "Closed" &&
                    ticket.status !== "Cancelled" && (
                        <form onSubmit={handleCommentSubmit}>
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

                            <button
                                type="submit"
                                disabled={
                                    isCommentSubmitting ||
                                    !commentContent.trim()
                                }
                            >
                                {isCommentSubmitting
                                    ? "Gönderiliyor..."
                                    : "Yorum Ekle"}
                            </button>

                            {commentError && (
                                <p role="alert">{commentError}</p>
                            )}
                        </form>
                    )}
            </section>
        </main>
    );
}
