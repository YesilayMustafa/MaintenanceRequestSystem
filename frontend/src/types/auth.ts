export type UserRole = "Employee" | "Technician" | "Admin";

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
}