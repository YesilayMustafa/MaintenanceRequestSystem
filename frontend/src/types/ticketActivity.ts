export type TicketActivityType =
    | "TicketCreated"
    | "AssignmentChanged"
    | "StatusChanged"
    | "PriorityChanged"
    | "CategoryChanged"
    | "CommentAdded"
    | "AttachmentUploaded"
    | string;

export interface TicketActivityDto {
    id: string;
    type: TicketActivityType;
    title: string;
    description: string;
    actorUserId: string;
    actorFullName: string;
    createdAt: string;
    commentId: string | null;
    attachmentId: string | null;
}
