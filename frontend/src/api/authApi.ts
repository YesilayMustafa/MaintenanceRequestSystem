import { apiRequest } from "./httpClient";
import type {
    AcceptInvitationRequest,
    ChangePasswordRequest,
    CurrentUser,
    ForgotPasswordRequest,
    ForgotPasswordResponse,
    LoginRequest,
    LoginResponse,
    ResetPasswordRequest,
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

export function acceptInvitation(
    request: AcceptInvitationRequest
): Promise<void> {
    return apiRequest<void>("/api/auth/invitations/accept", {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function forgotPassword(
    request: ForgotPasswordRequest
): Promise<ForgotPasswordResponse> {
    return apiRequest<ForgotPasswordResponse>("/api/auth/forgot-password", {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function resetPassword(
    request: ResetPasswordRequest
): Promise<void> {
    return apiRequest<void>("/api/auth/reset-password", {
        method: "POST",
        body: JSON.stringify(request),
    });
}

export function changePassword(
    token: string,
    request: ChangePasswordRequest
): Promise<void> {
    return apiRequest<void>("/api/auth/change-password", {
        method: "POST",
        token,
        body: JSON.stringify(request),
    });
}
