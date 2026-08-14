import {
    apiRequest,
    apiResponse,
} from "./httpClient";

import type {
    AttachmentDownload,
    TicketAttachmentDto,
} from "../types/attachments";

export function getTicketAttachments(
    token: string,
    ticketId: string
): Promise<TicketAttachmentDto[]> {
    return apiRequest<TicketAttachmentDto[]>(
        `/api/tickets/${ticketId}/attachments`,
        {
            method: "GET",
            token,
        }
    );
}

export function uploadTicketAttachment(
    token: string,
    ticketId: string,
    file: File
): Promise<TicketAttachmentDto> {
    const formData = new FormData();
    formData.append("file", file);

    return apiRequest<TicketAttachmentDto>(
        `/api/tickets/${ticketId}/attachments`,
        {
            method: "POST",
            token,
            body: formData,
        }
    );
}

export async function downloadTicketAttachment(
    token: string,
    ticketId: string,
    attachmentId: string,
    fallbackFileName: string
): Promise<AttachmentDownload> {
    const response = await apiResponse(
        `/api/tickets/${ticketId}/attachments/${attachmentId}/download`,
        {
            method: "GET",
            token,
        }
    );

    const headerFileName = getResponseFileName(
        response.headers.get("Content-Disposition")
    );

    return {
        blob: await response.blob(),
        fileName: sanitizeFileName(headerFileName ?? fallbackFileName),
    };
}

export function deleteTicketAttachment(
    token: string,
    ticketId: string,
    attachmentId: string
): Promise<void> {
    return apiRequest<void>(
        `/api/tickets/${ticketId}/attachments/${attachmentId}`,
        {
            method: "DELETE",
            token,
        }
    );
}

export function getResponseFileName(
    contentDisposition: string | null
): string | null {
    if (!contentDisposition) {
        return null;
    }

    const encodedMatch = contentDisposition.match(
        /filename\*=UTF-8''([^;]+)/i
    );

    if (encodedMatch?.[1]) {
        try {
            return decodeURIComponent(encodedMatch[1]);
        } catch {
            return null;
        }
    }

    const fileNameMatch = contentDisposition.match(
        /filename="?([^";]+)"?/i
    );

    return fileNameMatch?.[1] ?? null;
}

export function sanitizeFileName(fileName: string): string {
    const pathSegments = fileName.split(/[\\/]/);
    const normalizedName = pathSegments.at(-1)?.trim() ?? "";
    const invalidCharacters = '<>:"|?*';
    const safeName = Array.from(normalizedName)
        .map((character) =>
            character.charCodeAt(0) < 32 ||
            invalidCharacters.includes(character)
                ? "_"
                : character
        )
        .join("");

    return safeName || "attachment";
}
