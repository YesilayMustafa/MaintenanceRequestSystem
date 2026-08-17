import {
    useCallback,
    useEffect,
    useRef,
    useState,
} from "react";
import { Link, useNavigate } from "react-router-dom";

import {
    getNotifications,
    getUnreadNotificationCount,
    markAllNotificationsRead,
    markNotificationRead,
} from "../../api/notificationsApi";
import { ApiError } from "../../api/httpClient";
import { NotificationList } from "./NotificationList";
import { subscribeToNotificationsChanged } from "./notificationEvents";
import { Icon } from "../../components/Icon";

import type { NotificationDto } from "../../types/notifications";

const pollingIntervalMilliseconds = 30_000;

interface NotificationCenterProps {
    token: string;
}

export function NotificationCenter({ token }: NotificationCenterProps) {
    const navigate = useNavigate();
    const containerRef = useRef<HTMLDivElement>(null);
    const [isOpen, setIsOpen] = useState(false);
    const [unreadCount, setUnreadCount] = useState(0);
    const [notifications, setNotifications] = useState<NotificationDto[]>([]);
    const [isLoading, setIsLoading] = useState(false);
    const [isMarkingAll, setIsMarkingAll] = useState(false);
    const [activeNotificationId, setActiveNotificationId] =
        useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);

    const refreshUnreadCount = useCallback(async () => {
        try {
            const result = await getUnreadNotificationCount(token);
            setUnreadCount(result.count);
        } catch {
            // Geçici count hatası ana uygulama deneyimini kesmemelidir.
        }
    }, [token]);

    const loadNotifications = useCallback(async () => {
        try {
            setIsLoading(true);
            setError(null);
            const result = await getNotifications(token, {
                pageNumber: 1,
                pageSize: 10,
            });
            setNotifications(result.items);
        } catch (error) {
            setError(getErrorMessage(error, "Bildirimler yüklenemedi."));
        } finally {
            setIsLoading(false);
        }
    }, [token]);

    useEffect(() => {
        refreshUnreadCount();
        const interval = window.setInterval(
            refreshUnreadCount,
            pollingIntervalMilliseconds
        );

        return () => window.clearInterval(interval);
    }, [refreshUnreadCount]);

    useEffect(() => {
        return subscribeToNotificationsChanged(refreshUnreadCount);
    }, [refreshUnreadCount]);

    useEffect(() => {
        if (isOpen) {
            loadNotifications();
            refreshUnreadCount();
        }
    }, [isOpen, loadNotifications, refreshUnreadCount]);

    useEffect(() => {
        function handlePointerDown(event: PointerEvent) {
            if (
                containerRef.current &&
                !containerRef.current.contains(event.target as Node)
            ) {
                setIsOpen(false);
            }
        }

        document.addEventListener("pointerdown", handlePointerDown);
        return () => document.removeEventListener("pointerdown", handlePointerDown);
    }, []);

    async function handleOpen(notification: NotificationDto) {
        if (activeNotificationId) {
            return;
        }

        try {
            setActiveNotificationId(notification.id);
            setError(null);

            if (!notification.isRead) {
                await markNotificationRead(token, notification.id);
                setNotifications((items) => items.map((item) =>
                    item.id === notification.id
                        ? { ...item, isRead: true }
                        : item
                ));
                setUnreadCount((count) => Math.max(0, count - 1));
            }

            if (notification.ticketId && notification.ticketNumber) {
                setIsOpen(false);
                navigate(`/tickets/${notification.ticketId}`);
            }
        } catch (error) {
            setError(getErrorMessage(error, "Bildirim güncellenemedi."));
        } finally {
            setActiveNotificationId(null);
        }
    }

    async function handleMarkAllRead() {
        if (isMarkingAll || unreadCount === 0) {
            return;
        }

        try {
            setIsMarkingAll(true);
            setError(null);
            await markAllNotificationsRead(token);
            setUnreadCount(0);
            setNotifications((items) => items.map((item) => ({
                ...item,
                isRead: true,
            })));
        } catch (error) {
            setError(getErrorMessage(error, "Bildirimler güncellenemedi."));
        } finally {
            setIsMarkingAll(false);
        }
    }

    const badgeText = unreadCount > 99 ? "99+" : String(unreadCount);

    return (
        <div className="notification-center" ref={containerRef}>
            <button
                type="button"
                className="notification-bell"
                aria-label={
                    unreadCount > 0
                        ? `Bildirimler, ${unreadCount} okunmamış`
                        : "Bildirimler"
                }
                aria-expanded={isOpen}
                aria-controls="notification-panel"
                onClick={() => setIsOpen((open) => !open)}
            >
                <Icon name="bell" />
                {unreadCount > 0 && (
                    <span className="notification-badge">{badgeText}</span>
                )}
            </button>

            {isOpen && (
                <section
                    id="notification-panel"
                    className="notification-panel"
                    aria-labelledby="notification-panel-title"
                >
                    <div className="notification-panel-header">
                        <h2 id="notification-panel-title">Bildirimler</h2>
                        <button
                            type="button"
                            className="button button-small"
                            disabled={isMarkingAll || unreadCount === 0}
                            onClick={handleMarkAllRead}
                        >
                            {isMarkingAll ? "İşaretleniyor..." : "Tümünü Okundu İşaretle"}
                        </button>
                    </div>

                    {isLoading ? (
                        <p className="loading-state">Bildirimler yükleniyor...</p>
                    ) : (
                        <NotificationList
                            notifications={notifications}
                            activeNotificationId={activeNotificationId}
                            emptyMessage="Henüz bildiriminiz yok."
                            onOpen={handleOpen}
                        />
                    )}

                    {error && <p className="error-state" role="alert">{error}</p>}

                    <Link
                        to="/notifications"
                        className="notification-all-link"
                        onClick={() => setIsOpen(false)}
                    >
                        Tüm Bildirimleri Gör
                    </Link>
                </section>
            )}
        </div>
    );
}

function getErrorMessage(error: unknown, fallback: string): string {
    return error instanceof ApiError ? error.message : fallback;
}
