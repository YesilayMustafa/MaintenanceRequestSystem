import { apiRequest } from "./httpClient";

import type { DashboardDto } from "../types/dashboard";

export function getDashboard(token: string): Promise<DashboardDto> {
    return apiRequest<DashboardDto>("/api/dashboard", {
        method: "GET",
        token,
    });
}
