import { useEffect, useState } from "react";

import { ApiError } from "../../api/httpClient";
import { getCategories } from "../../api/categoriesApi";
import {
    assignTicket,
    cancelTicket,
    changeTicketCategory,
    changeTicketPriority,
    closeTicket,
    putOnHold,
    reassignTicket,
    reopenTicket,
    resolveTicket,
    resumeTicket,
    softDeleteTicket,
    startProgress,
} from "../../api/ticketsApi";
import { getUsers } from "../../api/usersApi";

import type { AuthenticatedUser } from "../../types/auth";
import type { TicketCategoryDto } from "../../types/categories";
import type {
    TicketDto,
    TicketPriority,
    TicketPriorityValue,
} from "../../types/tickets";
import type { UserDto } from "../../types/users";

interface TicketActionsProps {
    ticket: TicketDto;
    user: AuthenticatedUser;
    token: string;
    onTicketUpdated: (ticket: TicketDto) => Promise<void>;
    onSoftDeleted: () => void;
}

const priorityOptions: Array<{
    label: TicketPriority;
    value: TicketPriorityValue;
}> = [
    { label: "Low", value: 1 },
    { label: "Medium", value: 2 },
    { label: "High", value: 3 },
    { label: "Critical", value: 4 },
];

const priorityValues: Record<TicketPriority, TicketPriorityValue> = {
    Low: 1,
    Medium: 2,
    High: 3,
    Critical: 4,
};

export function TicketActions({
    ticket,
    user,
    token,
    onTicketUpdated,
    onSoftDeleted,
}: TicketActionsProps) {
    const [technicians, setTechnicians] = useState<UserDto[]>([]);
    const [categories, setCategories] = useState<TicketCategoryDto[]>([]);
    const [selectedTechnicianId, setSelectedTechnicianId] = useState("");
    const [selectedCategoryId, setSelectedCategoryId] = useState("");
    const [selectedPriority, setSelectedPriority] =
        useState<TicketPriorityValue>(priorityValues[ticket.priority]);
    const [waitingReason, setWaitingReason] = useState("");
    const [resolutionDescription, setResolutionDescription] = useState("");
    const [reopenReason, setReopenReason] = useState("");
    const [activeAction, setActiveAction] = useState<string | null>(null);
    const [actionError, setActionError] = useState<string | null>(null);
    const [actionSuccess, setActionSuccess] = useState<string | null>(null);
    const [isTechniciansLoading, setIsTechniciansLoading] = useState(false);
    const [isCategoriesLoading, setIsCategoriesLoading] = useState(false);

    const isAdmin = user.role === "Admin";
    const isTicketOwner =
        user.role === "Employee" &&
        ticket.createdByUserId === user.id;
    const isAssignedTechnician =
        user.role === "Technician" &&
        ticket.assignedTechnicianId === user.id;

    const needsTechnicianList =
        isAdmin &&
        (ticket.status === "Open" || ticket.status === "Assigned");

    useEffect(() => {
        let cancelled = false;

        async function loadTechnicians() {
            if (!needsTechnicianList) {
                return;
            }

            try {
                setIsTechniciansLoading(true);
                setActionError(null);

                const users = await getUsers(token);

                if (!cancelled) {
                    setTechnicians(
                        users.filter(
                            (candidate) =>
                                candidate.isActive &&
                                candidate.role === "Technician"
                        )
                    );
                }
            } catch (error) {
                if (cancelled) {
                    return;
                }

                setActionError(getErrorMessage(
                    error,
                    "Teknik personel listesi yüklenemedi."
                ));
            } finally {
                if (!cancelled) {
                    setIsTechniciansLoading(false);
                }
            }
        }

        loadTechnicians();

        return () => {
            cancelled = true;
        };
    }, [needsTechnicianList, token]);

    useEffect(() => {
        let cancelled = false;

        async function loadCategories() {
            if (!isAdmin) {
                return;
            }

            try {
                setIsCategoriesLoading(true);
                const result = await getCategories(token);

                if (!cancelled) {
                    setCategories(result);
                }
            } catch (error) {
                if (!cancelled) {
                    setActionError(getErrorMessage(
                        error,
                        "Kategori seçenekleri yüklenemedi."
                    ));
                }
            } finally {
                if (!cancelled) {
                    setIsCategoriesLoading(false);
                }
            }
        }

        loadCategories();

        return () => {
            cancelled = true;
        };
    }, [isAdmin, token]);

    useEffect(() => {
        setSelectedPriority(priorityValues[ticket.priority]);
    }, [ticket.priority]);

    useEffect(() => {
        setSelectedCategoryId("");
    }, [ticket.categoryId]);

    const technicianOptions =
        ticket.status === "Assigned"
            ? technicians.filter(
                (technician) =>
                    technician.id !== ticket.assignedTechnicianId
            )
            : technicians;

    const canChangePriority =
        isAdmin &&
        ["Open", "Assigned", "InProgress", "Waiting"]
            .includes(ticket.status);

    const canClose =
        (isAdmin || isTicketOwner) &&
        ticket.status === "Resolved";

    const canReopen =
        (isAdmin || isTicketOwner) &&
        ticket.status === "Closed";

    const canCancel =
        (isAdmin &&
            ["Open", "Assigned", "Waiting"].includes(ticket.status)) ||
        (isTicketOwner && ticket.status === "Open");

    const canSoftDelete =
        isAdmin &&
        (ticket.status === "Closed" || ticket.status === "Cancelled");

    const hasTechnicianAction =
        isAssignedTechnician &&
        ["Assigned", "InProgress", "Waiting"].includes(ticket.status);

    const hasAnyAction =
        isAdmin ||
        needsTechnicianList ||
        canChangePriority ||
        canClose ||
        canReopen ||
        canCancel ||
        canSoftDelete ||
        hasTechnicianAction;

    async function runTicketAction(
        actionName: string,
        action: () => Promise<TicketDto>,
        clearInput?: () => void
    ) {
        if (activeAction) {
            return;
        }

        try {
            setActiveAction(actionName);
            setActionError(null);
            setActionSuccess(null);

            const updatedTicket = await action();

            await onTicketUpdated(updatedTicket);
            clearInput?.();

            if (actionName === "category") {
                setActionSuccess("Talep kategorisi güncellendi.");
            }
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Ticket işlemi tamamlanamadı."
            ));
        } finally {
            setActiveAction(null);
        }
    }

    async function handleSoftDelete() {
        if (
            activeAction ||
            !window.confirm("Ticket pasifleştirilsin mi?")
        ) {
            return;
        }

        try {
            setActiveAction("soft-delete");
            setActionError(null);

            await softDeleteTicket(token, ticket.id);
            onSoftDeleted();
        } catch (error) {
            setActionError(getErrorMessage(
                error,
                "Ticket pasifleştirilemedi."
            ));
        } finally {
            setActiveAction(null);
        }
    }

    return (
        <section className="card action-card" aria-labelledby="actions-title">
            <h2 id="actions-title">İşlemler</h2>

            {actionError && (
                <p className="error-state" role="alert">{actionError}</p>
            )}

            {actionSuccess && (
                <p className="success-state" role="status">{actionSuccess}</p>
            )}

            {!hasAnyAction && (
                <p className="empty-state">
                    Bu ticket için kullanılabilir işlem bulunmuyor.
                </p>
            )}

            {needsTechnicianList && (
                <div className="action-group">
                    <label htmlFor="technician-select">
                        Teknik Personel
                    </label>

                    <select
                        id="technician-select"
                        value={selectedTechnicianId}
                        onChange={(event) =>
                            setSelectedTechnicianId(event.target.value)
                        }
                        disabled={
                            isTechniciansLoading ||
                            activeAction !== null
                        }
                    >
                        <option value="">Teknik personel seçin</option>

                        {technicianOptions.map((technician) => (
                            <option
                                key={technician.id}
                                value={technician.id}
                            >
                                {technician.fullName}
                            </option>
                        ))}
                    </select>

                    {ticket.status === "Open" && (
                        <button
                            type="button"
                            className="button button-primary"
                            disabled={
                                activeAction !== null ||
                                !selectedTechnicianId
                            }
                            onClick={() => runTicketAction(
                                "assign",
                                () => assignTicket(
                                    token,
                                    ticket.id,
                                    { technicianId: selectedTechnicianId }
                                ),
                                () => setSelectedTechnicianId("")
                            )}
                        >
                            Technician Ata
                        </button>
                    )}

                    {ticket.status === "Assigned" && (
                        <button
                            type="button"
                            className="button button-secondary"
                            disabled={
                                activeAction !== null ||
                                !selectedTechnicianId
                            }
                            onClick={() => runTicketAction(
                                "reassign",
                                () => reassignTicket(
                                    token,
                                    ticket.id,
                                    { technicianId: selectedTechnicianId }
                                ),
                                () => setSelectedTechnicianId("")
                            )}
                        >
                            Yeniden Ata
                        </button>
                    )}
                </div>
            )}

            {canChangePriority && (
                <div className="action-group">
                    <label htmlFor="priority-select">Öncelik</label>

                    <select
                        id="priority-select"
                        value={selectedPriority}
                        onChange={(event) =>
                            setSelectedPriority(
                                Number(event.target.value) as
                                    TicketPriorityValue
                            )
                        }
                        disabled={activeAction !== null}
                    >
                        {priorityOptions.map((option) => (
                            <option
                                key={option.value}
                                value={option.value}
                            >
                                {option.label}
                            </option>
                        ))}
                    </select>

                    <button
                        type="button"
                        className="button button-secondary"
                        disabled={
                            activeAction !== null ||
                            selectedPriority === priorityValues[ticket.priority]
                        }
                        onClick={() => runTicketAction(
                            "priority",
                            () => changeTicketPriority(
                                token,
                                ticket.id,
                                { priority: selectedPriority }
                            )
                        )}
                    >
                        Öncelik Değiştir
                    </button>
                </div>
            )}

            {isAdmin && (
                <div className="action-group">
                    <p className="muted-text">
                        Mevcut kategori: <strong>{ticket.categoryName}</strong>
                    </p>
                    <label htmlFor="category-select">
                        Kategoriyi Değiştir
                    </label>
                    <select
                        id="category-select"
                        value={selectedCategoryId}
                        onChange={(event) =>
                            setSelectedCategoryId(event.target.value)
                        }
                        disabled={
                            isCategoriesLoading ||
                            activeAction !== null
                        }
                    >
                        <option value="">
                            {isCategoriesLoading
                                ? "Kategoriler yükleniyor..."
                                : "Yeni kategori seçin"}
                        </option>
                        {categories
                            .filter((category) =>
                                category.id !== ticket.categoryId
                            )
                            .map((category) => (
                                <option
                                    key={category.id}
                                    value={category.id}
                                >
                                    {category.name}
                                </option>
                            ))}
                    </select>
                    <button
                        type="button"
                        className="button button-secondary"
                        disabled={
                            activeAction !== null ||
                            !selectedCategoryId
                        }
                        onClick={() => runTicketAction(
                            "category",
                            () => changeTicketCategory(
                                token,
                                ticket.id,
                                { categoryId: selectedCategoryId }
                            ),
                            () => setSelectedCategoryId("")
                        )}
                    >
                        Kategoriyi Değiştir
                    </button>
                </div>
            )}

            {isAssignedTechnician && ticket.status === "Assigned" && (
                <button
                    type="button"
                    className="button button-primary"
                    disabled={activeAction !== null}
                    onClick={() => runTicketAction(
                        "start-progress",
                        () => startProgress(token, ticket.id)
                    )}
                >
                    İşleme Al
                </button>
            )}

            {isAssignedTechnician && ticket.status === "InProgress" && (
                <>
                    <div className="action-group">
                        <label htmlFor="waiting-reason">
                            Bekleme Nedeni
                        </label>

                        <textarea
                            id="waiting-reason"
                            value={waitingReason}
                            onChange={(event) =>
                                setWaitingReason(event.target.value)
                            }
                            disabled={activeAction !== null}
                        />

                        <button
                            type="button"
                            className="button button-secondary"
                            disabled={
                                activeAction !== null ||
                                !waitingReason.trim()
                            }
                            onClick={() => runTicketAction(
                                "put-on-hold",
                                () => putOnHold(
                                    token,
                                    ticket.id,
                                    { reason: waitingReason.trim() }
                                ),
                                () => setWaitingReason("")
                            )}
                        >
                            Beklemeye Al
                        </button>
                    </div>

                    <div className="action-group">
                        <label htmlFor="resolution-description">
                            Çözüm Açıklaması
                        </label>

                        <textarea
                            id="resolution-description"
                            value={resolutionDescription}
                            onChange={(event) =>
                                setResolutionDescription(event.target.value)
                            }
                            disabled={activeAction !== null}
                        />

                        <button
                            type="button"
                            className="button button-primary"
                            disabled={
                                activeAction !== null ||
                                !resolutionDescription.trim()
                            }
                            onClick={() => runTicketAction(
                                "resolve",
                                () => resolveTicket(
                                    token,
                                    ticket.id,
                                    {
                                        resolutionDescription:
                                            resolutionDescription.trim(),
                                    }
                                ),
                                () => setResolutionDescription("")
                            )}
                        >
                            Çöz
                        </button>
                    </div>
                </>
            )}

            {isAssignedTechnician && ticket.status === "Waiting" && (
                <button
                    type="button"
                    className="button button-primary"
                    disabled={activeAction !== null}
                    onClick={() => runTicketAction(
                        "resume",
                        () => resumeTicket(token, ticket.id)
                    )}
                >
                    Devam Et
                </button>
            )}

            {canClose && (
                <button
                    type="button"
                    className="button button-primary"
                    disabled={activeAction !== null}
                    onClick={() => runTicketAction(
                        "close",
                        () => closeTicket(token, ticket.id)
                    )}
                >
                    Kapat
                </button>
            )}

            {canReopen && (
                <div className="action-group">
                    <label htmlFor="reopen-reason">
                        Yeniden Açma Nedeni
                    </label>

                    <textarea
                        id="reopen-reason"
                        value={reopenReason}
                        onChange={(event) =>
                            setReopenReason(event.target.value)
                        }
                        disabled={activeAction !== null}
                    />

                    <button
                        type="button"
                        className="button button-secondary"
                        disabled={
                            activeAction !== null ||
                            !reopenReason.trim()
                        }
                        onClick={() => runTicketAction(
                            "reopen",
                            () => reopenTicket(
                                token,
                                ticket.id,
                                { reason: reopenReason.trim() }
                            ),
                            () => setReopenReason("")
                        )}
                    >
                        Yeniden Aç
                    </button>
                </div>
            )}

            {canCancel && (
                <button
                    type="button"
                    className="button button-danger"
                    disabled={activeAction !== null}
                    onClick={() => runTicketAction(
                        "cancel",
                        () => cancelTicket(token, ticket.id)
                    )}
                >
                    İptal Et
                </button>
            )}

            {canSoftDelete && (
                <button
                    type="button"
                    className="button button-danger"
                    disabled={activeAction !== null}
                    onClick={handleSoftDelete}
                >
                    Pasifleştir
                </button>
            )}
        </section>
    );
}

function getErrorMessage(
    error: unknown,
    fallbackMessage: string
): string {
    return error instanceof ApiError
        ? error.message
        : fallbackMessage;
}
