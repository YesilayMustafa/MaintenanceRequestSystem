import {
    useCallback,
    useEffect,
    useState,
    type ReactNode,
} from "react";

import {
    getCurrentUser,
    login as loginRequest,
} from "../api/authApi";

import type {
    AuthenticatedUser,
    LoginRequest,
} from "../types/auth";
import {
    AuthContext,
    type AuthContextValue,
} from "./AuthContext";

const TOKEN_KEY = "mrs_access_token";
const EXPIRES_AT_KEY = "mrs_expires_at";

interface AuthProviderProps {
    children: ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
    const [user, setUser] = useState<AuthenticatedUser | null>(null);
    const [token, setToken] = useState<string | null>(null);
    const [expiresAt, setExpiresAt] = useState<string | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    const clearSession = useCallback(() => {
        sessionStorage.removeItem(TOKEN_KEY);
        sessionStorage.removeItem(EXPIRES_AT_KEY);

        setToken(null);
        setExpiresAt(null);
        setUser(null);
    }, []);

    const login = useCallback(async (request: LoginRequest) => {
        const response = await loginRequest(request);

        sessionStorage.setItem(TOKEN_KEY, response.accessToken);
        sessionStorage.setItem(EXPIRES_AT_KEY, response.expiresAt);

        setToken(response.accessToken);
        setExpiresAt(response.expiresAt);
        setUser(response.user);
    }, []);

    const logout = useCallback(() => {
        clearSession();
    }, [clearSession]);

    useEffect(() => {
        let cancelled = false;

        async function restoreSession() {
            const storedToken = sessionStorage.getItem(TOKEN_KEY);
            const storedExpiresAt = sessionStorage.getItem(EXPIRES_AT_KEY);

            if (!storedToken || !storedExpiresAt) {
                setIsLoading(false);
                return;
            }

            const expirationTime = Date.parse(storedExpiresAt);

            if (
                Number.isNaN(expirationTime) ||
                expirationTime <= Date.now()
            ) {
                clearSession();
                setIsLoading(false);
                return;
            }

            try {
                const currentUser = await getCurrentUser(storedToken);

                if (cancelled) {
                    return;
                }

                setToken(storedToken);
                setExpiresAt(storedExpiresAt);
                setUser(currentUser);
            } catch {
                if (!cancelled) {
                    clearSession();
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        restoreSession();

        return () => {
            cancelled = true;
        };
    }, [clearSession]);

    useEffect(() => {
        if (!expiresAt) {
            return;
        }

        const expirationTime = Date.parse(expiresAt);
        const remainingTime = expirationTime - Date.now();

        if (remainingTime <= 0) {
            clearSession();
            return;
        }

        const timer = window.setTimeout(() => {
            clearSession();
        }, remainingTime);

        return () => {
            window.clearTimeout(timer);
        };
    }, [expiresAt, clearSession]);

    const value: AuthContextValue = {
        user,
        token,
        isAuthenticated: Boolean(user && token),
        isLoading,
        login,
        logout,
    };

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}
