import { apiRequest } from "./httpClient";

import type {
    ChangeTicketCategoryStatusRequest,
    CreateTicketCategoryRequest,
    TicketCategoryDto,
    UpdateTicketCategoryRequest,
} from "../types/categories";

export function getCategories(
    token: string,
    includeInactive = false
): Promise<TicketCategoryDto[]> {
    const searchParams = new URLSearchParams();

    if (includeInactive) {
        searchParams.set("includeInactive", "true");
    }

    const query = searchParams.toString();

    return apiRequest<TicketCategoryDto[]>(
        `/api/categories${query ? `?${query}` : ""}`,
        {
            method: "GET",
            token,
        }
    );
}

export function getCategory(
    token: string,
    id: string
): Promise<TicketCategoryDto> {
    return apiRequest<TicketCategoryDto>(
        `/api/categories/${id}`,
        {
            method: "GET",
            token,
        }
    );
}

export function createCategory(
    token: string,
    request: CreateTicketCategoryRequest
): Promise<TicketCategoryDto> {
    return apiRequest<TicketCategoryDto>(
        "/api/categories",
        {
            method: "POST",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function updateCategory(
    token: string,
    id: string,
    request: UpdateTicketCategoryRequest
): Promise<TicketCategoryDto> {
    return apiRequest<TicketCategoryDto>(
        `/api/categories/${id}`,
        {
            method: "PUT",
            token,
            body: JSON.stringify(request),
        }
    );
}

export function changeCategoryStatus(
    token: string,
    id: string,
    request: ChangeTicketCategoryStatusRequest
): Promise<void> {
    return apiRequest<void>(
        `/api/categories/${id}/status`,
        {
            method: "PATCH",
            token,
            body: JSON.stringify(request),
        }
    );
}
