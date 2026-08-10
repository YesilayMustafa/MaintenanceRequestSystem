import { apiRequest } from "./httpClient";

import type { UserDto } from "../types/users";

export function getUsers(token: string): Promise<UserDto[]> {
    return apiRequest<UserDto[]>(
        "/api/users",
        {
            method: "GET",
            token,
        }
    );
}
