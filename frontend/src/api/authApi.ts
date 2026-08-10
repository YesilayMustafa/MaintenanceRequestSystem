import { apiRequest } from "./httpClient";
import type {
    CurrentUser,
    LoginRequest,
    LoginResponse,
} from "../types/auth";

export function login(request: LoginRequest): Promise<LoginResponse> {
    return apiRequest<LoginResponse>("/api/auth/login", {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function getCurrentUser(token: string): Promise<CurrentUser> {
    return apiRequest<CurrentUser>("/api/auth/me", {
        method: "GET",
        token,
    });
}