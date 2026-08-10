import { Link } from "react-router-dom";

import { useAuth } from "../auth/useAuth";

export function AppNavigation() {
    const { user } = useAuth();

    return (
        <nav>
            <Link to="/tickets">Ticketlar</Link>
            {" | "}
            <Link to="/assets">Cihazlar</Link>
            {" | "}
            <Link to="/departments">Departmanlar</Link>

            {user?.role === "Admin" && (
                <>
                    {" | "}
                    <Link to="/users">Kullanıcılar</Link>
                    {" | "}
                    <Link to="/audit-logs">Audit Logları</Link>
                </>
            )}
        </nav>
    );
}
