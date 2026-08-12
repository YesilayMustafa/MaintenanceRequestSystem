import { notifyUnauthorized } from "../auth/authSession";

const API_BASE_URL = normalizeBaseUrl(
    import.meta.env.VITE_API_BASE_URL
);

export interface ProblemDetails {
    status?: number;
    title?: string;
    detail?: string;
    instance?: string;
    traceId?: string;
}

export class ApiError extends Error {
    status: number;
    problemDetails?: ProblemDetails;

    constructor(
        status: number,
        message: string,
        problemDetails?: ProblemDetails
    ) {
        super(message);

        this.name = "ApiError";
        this.status = status;
        this.problemDetails = problemDetails;
    }
}

interface RequestOptions extends RequestInit {
    token?: string | null;
}

export async function apiRequest<T>(
    path: string,
    options: RequestOptions = {}
): Promise<T> {
    const { token, headers, ...requestOptions } = options;

    const requestHeaders = new Headers(headers);

    if (!requestHeaders.has("Content-Type") && requestOptions.body) {
        requestHeaders.set("Content-Type", "application/json");
    }

    if (token) {
        requestHeaders.set("Authorization", `Bearer ${token}`);
    }

    const response = await fetch(buildApiUrl(path), {
        ...requestOptions,
        headers: requestHeaders,
    });

    if (response.status === 401 && token) {
        notifyUnauthorized();
    }

    if (!response.ok) {
        let problemDetails: ProblemDetails | undefined;

        try {
            problemDetails = (await response.json()) as ProblemDetails;
        } catch {
            problemDetails = undefined;
        }

        throw new ApiError(
            response.status,
            problemDetails?.detail ??
            problemDetails?.title ??
            getDefaultErrorMessage(response.status),
            problemDetails
        );
    }

    if (response.status === 204) {
        return undefined as T;
    }

    return (await response.json()) as T;
}

function getDefaultErrorMessage(status: number): string {
    if (status === 409) {
        return "İşlem mevcut hesap durumuyla çakıştığı için tamamlanamadı.";
    }

    if (status === 429) {
        return "Çok fazla istek gönderildi. Lütfen kısa bir süre sonra tekrar deneyin.";
    }

    if (status === 503) {
        return "İşlem şu anda tamamlanamıyor. Lütfen daha sonra tekrar deneyin.";
    }

    return `API isteği başarısız oldu (${status}).`;
}

function normalizeBaseUrl(value: string | undefined): string {
    return value?.trim().replace(/\/+$/, "") ?? "";
}

function buildApiUrl(path: string): string {
    const normalizedPath = path.startsWith("/")
        ? path
        : `/${path}`;

    return `${API_BASE_URL}${normalizedPath}`;
}
