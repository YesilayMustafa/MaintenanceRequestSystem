import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";

import {
    getNotifications,
    markAllNotificationsRead,
    markNotificationRead,
} from "../api/notificationsApi";
import { ApiError } from "../api/httpClient";
import { useAuth } from "../auth/useAuth";
import { NotificationList } from "../features/notifications/NotificationList";
import { notifyNotificationsChanged } from "../features/notifications/notificationEvents";

import type { NotificationDto } from "../types/notifications";
import type { PagedResult } from "../types/pagination";

const pageSize = 10;

export function NotificationsPage() {
    const { token } = useAuth();
    const navigate = useNavigate();
    const [result, setResult] = useState<PagedResult<NotificationDto> | null>(null);
    const [pageNumber, setPageNumber] = useState(1);
    const [unreadOnly, setUnreadOnly] = useState(false);
    const [isLoading, setIsLoading] = useState(true);
    const [isMarkingAll, setIsMarkingAll] = useState(false);
    const [activeNotificationId, setActiveNotificationId] =
        useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function load() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setError(null);
                const response = await getNotifications(token, {
                    pageNumber,
                    pageSize,
                    unreadOnly,
                });

                if (!cancelled) {
                    setResult(response);
                }
            } catch (error) {
                if (!cancelled) {
                    setError(getErrorMessage(error, "Bildirimler yüklenemedi."));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        load();
        return () => { cancelled = true; };
    }, [pageNumber, token, unreadOnly]);

    async function handleOpen(notification: NotificationDto) {
        if (!token || activeNotificationId) {
            return;
        }

        try {
            setActiveNotificationId(notification.id);
            setError(null);

            if (!notification.isRead) {
                await markNotificationRead(token, notification.id);
                notifyNotificationsChanged();
                setResult((current) => current
                    ? {
                        ...current,
                        items: current.items.map((item) =>
                            item.id === notification.id
                                ? { ...item, isRead: true }
                                : item
                        ),
                    }
                    : current
                );
            }

            if (notification.ticketId && notification.ticketNumber) {
                navigate(`/tickets/${notification.ticketId}`);
            }
        } catch (error) {
            setError(getErrorMessage(error, "Bildirim güncellenemedi."));
        } finally {
            setActiveNotificationId(null);
        }
    }

    async function handleMarkAllRead() {
        if (!token || isMarkingAll) {
            return;
        }

        try {
            setIsMarkingAll(true);
            setError(null);
            await markAllNotificationsRead(token);
            notifyNotificationsChanged();

            if (unreadOnly) {
                setPageNumber(1);
                setResult((current) => current
                    ? { ...current, items: [], totalCount: 0, totalPages: 0 }
                    : current
                );
            } else {
                setResult((current) => current
                    ? {
                        ...current,
                        items: current.items.map((item) => ({
                            ...item,
                            isRead: true,
                        })),
                    }
                    : current
                );
            }
        } catch (error) {
            setError(getErrorMessage(error, "Bildirimler güncellenemedi."));
        } finally {
            setIsMarkingAll(false);
        }
    }

    return (
        <div className="page notification-page">
            <header className="page-header">
                <div>
                    <h1 className="page-title">Bildirimler</h1>
                    <p className="page-description">
                        Talep süreçlerinizle ilgili güncellemeleri takip edin.
                    </p>
                </div>
                <button
                    type="button"
                    className="button button-secondary"
                    disabled={isMarkingAll}
                    onClick={handleMarkAllRead}
                >
                    {isMarkingAll ? "İşaretleniyor..." : "Tümünü Okundu İşaretle"}
                </button>
            </header>

            <div className="notification-filter" role="group" aria-label="Bildirim filtresi">
                <button
                    type="button"
                    className={!unreadOnly ? "button button-primary" : "button"}
                    onClick={() => { setUnreadOnly(false); setPageNumber(1); }}
                >
                    Tümü
                </button>
                <button
                    type="button"
                    className={unreadOnly ? "button button-primary" : "button"}
                    onClick={() => { setUnreadOnly(true); setPageNumber(1); }}
                >
                    Okunmamış
                </button>
            </div>

            {isLoading ? (
                <p className="loading-state">Bildirimler yükleniyor...</p>
            ) : error ? (
                <p className="error-state" role="alert">{error}</p>
            ) : (
                <section className="card">
                    <NotificationList
                        notifications={result?.items ?? []}
                        activeNotificationId={activeNotificationId}
                        emptyMessage={
                            unreadOnly
                                ? "Okunmamış bildiriminiz bulunmuyor."
                                : "Henüz bildiriminiz yok."
                        }
                        onOpen={handleOpen}
                    />
                </section>
            )}

            {result && result.totalPages > 1 && (
                <nav className="pagination" aria-label="Bildirim sayfaları">
                    <p className="pagination-summary">
                        Sayfa {result.pageNumber} / {result.totalPages}
                    </p>
                    <div className="pagination-actions">
                        <button
                            type="button"
                            disabled={pageNumber <= 1 || isLoading}
                            onClick={() => setPageNumber((page) => page - 1)}
                        >
                            Önceki
                        </button>
                        <button
                            type="button"
                            disabled={pageNumber >= result.totalPages || isLoading}
                            onClick={() => setPageNumber((page) => page + 1)}
                        >
                            Sonraki
                        </button>
                    </div>
                </nav>
            )}
        </div>
    );
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof ApiError ? error.message : fallback;
}
