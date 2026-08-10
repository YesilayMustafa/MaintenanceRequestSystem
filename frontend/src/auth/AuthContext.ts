import { createContext } from "react";

import type {
    AuthenticatedUser,
    LoginRequest,
} from "../types/auth";

export interface AuthContextValue {
    user: AuthenticatedUser | null;
    token: string | null;
    isAuthenticated: boolean;
    isLoading: boolean;
    login: (request: LoginRequest) => Promise<void>;
    logout: () => void;
}

export const AuthContext = createContext<AuthContextValue | undefined>(
    undefined
);
