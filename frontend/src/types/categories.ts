export interface TicketCategoryDto {
    id: string;
    name: string;
    description: string | null;
    isActive: boolean;
    createdAt: string;
    updatedAt: string | null;
}

export interface CreateTicketCategoryRequest {
    name: string;
    description: string | null;
}

export type UpdateTicketCategoryRequest = CreateTicketCategoryRequest;

export interface ChangeTicketCategoryStatusRequest {
    isActive: boolean;
}
