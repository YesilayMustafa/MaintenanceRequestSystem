export type UserRole = "Employee" | "Technician" | "Admin";

export type AccountStatus =
  | "Active"
  | "PendingInvitation"
  | "Inactive";

export interface AuthenticatedUser {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  expiresAt: string;
  user: AuthenticatedUser;
}

export interface CurrentUser {
  id: string;
  fullName: string;
  email: string;
  role: UserRole;
  departmentId: string;
  departmentName: string;
  isActive: boolean;
  accountStatus: AccountStatus;
}

export interface AcceptInvitationRequest {
  token: string;
  newPassword: string;
}

export interface ForgotPasswordRequest {
  email: string;
}

export interface ForgotPasswordResponse {
  message: string;
}

export interface ResetPasswordRequest {
  token: string;
  newPassword: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
