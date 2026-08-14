import {
    useEffect,
    useRef,
    useState,
    type ChangeEvent,
} from "react";

import {
    deleteTicketAttachment,
    downloadTicketAttachment,
    getTicketAttachments,
    uploadTicketAttachment,
} from "../../api/attachmentsApi";
import { ApiError } from "../../api/httpClient";

import type { TicketAttachmentDto } from "../../types/attachments";
import type { AuthenticatedUser } from "../../types/auth";
import type { TicketStatus } from "../../types/tickets";

const maximumFileSizeBytes = 10 * 1024 * 1024;
const allowedExtensions = new Set([
    ".jpg",
    ".jpeg",
    ".png",
    ".webp",
    ".pdf",
]);
const acceptedFileTypes = ".jpg,.jpeg,.png,.webp,.pdf";

interface TicketAttachmentsProps {
    ticketId: string;
    ticketStatus: TicketStatus;
    token: string;
    user: AuthenticatedUser;
    onChanged?: () => void;
}

export function TicketAttachments({
    ticketId,
    ticketStatus,
    token,
    user,
    onChanged,
}: TicketAttachmentsProps) {
    const fileInputRef = useRef<HTMLInputElement>(null);
    const [attachments, setAttachments] = useState<TicketAttachmentDto[]>([]);
    const [selectedFile, setSelectedFile] = useState<File | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isUploading, setIsUploading] = useState(false);
    const [downloadingId, setDownloadingId] = useState<string | null>(null);
    const [deletingId, setDeletingId] = useState<string | null>(null);
    const [error, setError] = useState<string | null>(null);
    const [statusMessage, setStatusMessage] = useState<string | null>(null);

    const isUploadBlocked =
        ticketStatus === "Closed" || ticketStatus === "Cancelled";

    useEffect(() => {
        let cancelled = false;

        async function loadAttachments() {
            try {
                setIsLoading(true);
                setError(null);

                const result = await getTicketAttachments(token, ticketId);

                if (!cancelled) {
                    setAttachments(result);
                }
            } catch (error) {
                if (!cancelled) {
                    setError(getErrorMessage(
                        error,
                        "Ekler yüklenirken beklenmeyen bir hata oluştu."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadAttachments();

        return () => {
            cancelled = true;
        };
    }, [ticketId, token]);

    function handleFileChange(event: ChangeEvent<HTMLInputElement>) {
        const file = event.target.files?.[0] ?? null;

        setSelectedFile(null);
        setStatusMessage(null);
        setError(null);

        if (!file) {
            return;
        }

        const validationError = validateFile(file);

        if (validationError) {
            setError(validationError);
            event.target.value = "";
            return;
        }

        setSelectedFile(file);
    }

    async function handleUpload() {
        if (!selectedFile || isUploading || isUploadBlocked) {
            return;
        }

        try {
            setIsUploading(true);
            setError(null);
            setStatusMessage(null);

            const uploadedAttachment = await uploadTicketAttachment(
                token,
                ticketId,
                selectedFile
            );

            setAttachments((currentAttachments) => [
                ...currentAttachments,
                uploadedAttachment,
            ]);
            setSelectedFile(null);
            setStatusMessage("Dosya başarıyla yüklendi.");
            onChanged?.();

            if (fileInputRef.current) {
                fileInputRef.current.value = "";
            }
        } catch (error) {
            setError(getErrorMessage(
                error,
                "Dosya yüklenirken beklenmeyen bir hata oluştu."
            ));
        } finally {
            setIsUploading(false);
        }
    }

    async function handleDownload(attachment: TicketAttachmentDto) {
        try {
            setDownloadingId(attachment.id);
            setError(null);
            setStatusMessage(null);

            const download = await downloadTicketAttachment(
                token,
                ticketId,
                attachment.id,
                attachment.originalFileName
            );
            const objectUrl = URL.createObjectURL(download.blob);

            try {
                const link = document.createElement("a");
                link.href = objectUrl;
                link.download = download.fileName;
                document.body.appendChild(link);
                link.click();
                link.remove();
            } finally {
                URL.revokeObjectURL(objectUrl);
            }
        } catch (error) {
            setError(getErrorMessage(
                error,
                "Dosya indirilirken beklenmeyen bir hata oluştu."
            ));
        } finally {
            setDownloadingId(null);
        }
    }

    async function handleDelete(attachment: TicketAttachmentDto) {
        if (
            !window.confirm(
                `“${attachment.originalFileName}” dosyası silinsin mi?`
            )
        ) {
            return;
        }

        try {
            setDeletingId(attachment.id);
            setError(null);
            setStatusMessage(null);

            await deleteTicketAttachment(token, ticketId, attachment.id);

            setAttachments((currentAttachments) =>
                currentAttachments.filter(
                    (currentAttachment) =>
                        currentAttachment.id !== attachment.id
                )
            );
            setStatusMessage("Dosya başarıyla silindi.");
            onChanged?.();
        } catch (error) {
            setError(getErrorMessage(
                error,
                "Dosya silinirken beklenmeyen bir hata oluştu."
            ));
        } finally {
            setDeletingId(null);
        }
    }

    return (
        <section className="card" aria-labelledby="attachments-title">
            <div className="card-header">
                <div>
                    <h2 id="attachments-title">Ekler</h2>
                    <p className="muted-text">
                        JPG, PNG, WEBP veya PDF; en fazla 10 MB.
                    </p>
                </div>
            </div>

            {isLoading ? (
                <p className="loading-state">Ekler yükleniyor...</p>
            ) : attachments.length === 0 ? (
                <p className="empty-state">
                    Bu talebe henüz dosya eklenmemiş.
                </p>
            ) : (
                <ul className="attachment-list">
                    {attachments.map((attachment) => {
                        const canDelete =
                            user.role === "Admin" ||
                            user.id === attachment.uploadedByUserId;

                        return (
                            <li
                                className="attachment-item"
                                key={attachment.id}
                            >
                                <div className="attachment-details">
                                    <strong className="attachment-name">
                                        {attachment.originalFileName}
                                    </strong>
                                    <span className="attachment-meta">
                                        {getFileTypeLabel(
                                            attachment.originalFileName
                                        )}
                                        {" · "}
                                        {formatFileSize(attachment.sizeBytes)}
                                        {" · "}
                                        {attachment.uploadedByFullName}
                                        {" · "}
                                        {new Date(attachment.createdAt)
                                            .toLocaleString("tr-TR")}
                                    </span>
                                </div>

                                <div className="attachment-actions">
                                    <button
                                        type="button"
                                        className="button button-small"
                                        disabled={
                                            downloadingId === attachment.id ||
                                            deletingId !== null
                                        }
                                        onClick={() => handleDownload(attachment)}
                                    >
                                        {downloadingId === attachment.id
                                            ? "İndiriliyor..."
                                            : "İndir"}
                                    </button>

                                    {canDelete && (
                                        <button
                                            type="button"
                                            className="button button-danger button-small"
                                            disabled={
                                                deletingId === attachment.id ||
                                                downloadingId !== null
                                            }
                                            onClick={() => handleDelete(attachment)}
                                        >
                                            {deletingId === attachment.id
                                                ? "Siliniyor..."
                                                : "Sil"}
                                        </button>
                                    )}
                                </div>
                            </li>
                        );
                    })}
                </ul>
            )}

            {isUploadBlocked ? (
                <p className="attachment-upload-note">
                    Kapalı veya iptal edilmiş taleplere yeni dosya eklenemez.
                </p>
            ) : (
                <div className="attachment-upload">
                    <label htmlFor="ticket-attachment-file">Dosya Seç</label>
                    <input
                        ref={fileInputRef}
                        id="ticket-attachment-file"
                        type="file"
                        accept={acceptedFileTypes}
                        disabled={isUploading}
                        onChange={handleFileChange}
                    />

                    {selectedFile && (
                        <p className="selected-file" role="status">
                            <strong>{selectedFile.name}</strong>
                            {" · "}
                            {formatFileSize(selectedFile.size)}
                        </p>
                    )}

                    <button
                        type="button"
                        className="button button-primary"
                        disabled={!selectedFile || isUploading}
                        onClick={handleUpload}
                    >
                        {isUploading ? "Yükleniyor..." : "Yükle"}
                    </button>
                </div>
            )}

            {error && (
                <p className="error-state" role="alert">{error}</p>
            )}

            {statusMessage && (
                <p className="success-state" role="status">
                    {statusMessage}
                </p>
            )}
        </section>
    );
}

function validateFile(file: File): string | null {
    const extension = getFileExtension(file.name);

    if (!allowedExtensions.has(extension)) {
        return "Bu dosya türü desteklenmiyor. JPG, JPEG, PNG, WEBP veya PDF seçin.";
    }

    if (file.size > maximumFileSizeBytes) {
        return "Dosya boyutu 10 MB sınırını aşamaz.";
    }

    return null;
}

function getFileExtension(fileName: string): string {
    const extensionIndex = fileName.lastIndexOf(".");

    return extensionIndex >= 0
        ? fileName.slice(extensionIndex).toLowerCase()
        : "";
}

function getFileTypeLabel(fileName: string): string {
    const extension = getFileExtension(fileName).slice(1);

    return extension ? extension.toLocaleUpperCase("tr-TR") : "DOSYA";
}

function formatFileSize(sizeBytes: number): string {
    if (sizeBytes < 1024) {
        return `${sizeBytes} B`;
    }

    const kiloBytes = sizeBytes / 1024;

    if (kiloBytes < 1024) {
        return `${formatNumber(kiloBytes)} KB`;
    }

    return `${formatNumber(kiloBytes / 1024)} MB`;
}

function formatNumber(value: number): string {
    return new Intl.NumberFormat("tr-TR", {
        maximumFractionDigits: 1,
    }).format(value);
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
