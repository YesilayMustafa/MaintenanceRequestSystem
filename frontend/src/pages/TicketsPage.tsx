import { useEffect, useState } from "react";

import { ApiError } from "../api/httpClient";
import { getTickets } from "../api/ticketsApi";
import { useAuth } from "../auth/useAuth";

import type { TicketDto } from "../types/tickets";
import { Link } from "react-router-dom";

export function TicketsPage() {
    const { user, token, logout } = useAuth();

    const [tickets, setTickets] = useState<TicketDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let cancelled = false;

        async function loadTickets() {
            if (!token) {
                return;
            }

            try {
                setIsLoading(true);
                setError(null);

                const result = await getTickets(token, {
                    pageNumber: 1,
                    pageSize: 10,
                    sortBy: "createdAt",
                    sortDescending: true,
                });

                if (!cancelled) {
                    setTickets(result.items);
                }
            } catch (error) {
                if (cancelled) {
                    return;
                }

                if (error instanceof ApiError) {
                    setError(error.message);
                } else {
                    setError("Ticketlar yüklenirken beklenmeyen bir hata oluştu.");
                }
            } finally {
                if (!cancelled) {
                    setIsLoading(false);
                }
            }
        }

        loadTickets();

        return () => {
            cancelled = true;
        };
    }, [token]);

    return (
        <main>
            <h1>Ticketlar</h1>

            <p>
                Hoş geldin, <strong>{user?.fullName}</strong>
            </p>

            <p>Rol: {user?.role}</p>

            <button type="button" onClick={logout}>
                Çıkış Yap
            </button>

            <hr />

            <h2>Talep Listesi</h2>

            {isLoading && <p>Ticketlar yükleniyor...</p>}

            {error && (
                <p role="alert">
                    {error}
                </p>
            )}

            {!isLoading && !error && tickets.length === 0 && (
                <p>Gösterilecek ticket bulunamadı.</p>
            )}

            {!isLoading && !error && tickets.length > 0 && (
                <table>
                    <thead>
                        <tr>
                            <th>Başlık</th>
                            <th>Durum</th>
                            <th>Öncelik</th>
                            <th>Cihaz</th>
                            <th>Oluşturan</th>
                            <th>Teknisyen</th>
                            <th>Oluşturulma</th>
                        </tr>
                    </thead>

                    <tbody>
                        {tickets.map((ticket) => (
                            <tr key={ticket.id}>
                                <td>
                                    <Link to={`/tickets/${ticket.id}`}>
                                        {ticket.title}
                                    </Link>
                                </td>
                                <td>{ticket.status}</td>
                                <td>{ticket.priority}</td>
                                <td>{ticket.assetName}</td>
                                <td>{ticket.createdByFullName}</td>
                                <td>
                                    {ticket.assignedTechnicianFullName ?? "Atanmadı"}
                                </td>
                                <td>
                                    {new Date(ticket.createdAt).toLocaleString("tr-TR")}
                                </td>
                            </tr>
                        ))}
                    </tbody>
                </table>
            )}
        </main>
    );
}