using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    private static Ticket CreateTicket()
    {
        return new Ticket(
            "REQ-2000-999999",
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test talebi",
            "Test açıklaması",
            TicketPriority.Medium);
    }
}
