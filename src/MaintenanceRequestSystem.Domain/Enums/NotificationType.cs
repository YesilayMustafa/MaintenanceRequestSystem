namespace MaintenanceRequestSystem.Domain.Enums;

public enum NotificationType
{
    TicketAssigned = 1,
    TicketReassigned = 2,
    TicketStatusChanged = 3,
    TicketResolved = 4,
    TicketClosed = 5,
    TicketReopened = 6,
    TicketCancelled = 7,
    TicketCommentAdded = 8,
    TicketPriorityChanged = 9,
    TicketCategoryChanged = 10,
    TicketCreated = 11
}
