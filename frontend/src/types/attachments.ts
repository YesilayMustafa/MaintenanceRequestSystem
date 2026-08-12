export interface TicketAttachmentDto {
    id: string;
    ticketId: string;
    originalFileName: string;
    contentType: string;
    sizeBytes: number;
    uploadedByUserId: string;
    uploadedByFullName: string;
    createdAt: string;
}

export interface AttachmentDownload {
    blob: Blob;
    fileName: string;
}
