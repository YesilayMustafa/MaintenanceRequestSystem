const notificationsChangedEvent = "mrs:notifications-changed";

export function notifyNotificationsChanged() {
    window.dispatchEvent(new Event(notificationsChangedEvent));
}

export function subscribeToNotificationsChanged(
    handler: () => void
): () => void {
    window.addEventListener(notificationsChangedEvent, handler);

    return () => window.removeEventListener(notificationsChangedEvent, handler);
}
