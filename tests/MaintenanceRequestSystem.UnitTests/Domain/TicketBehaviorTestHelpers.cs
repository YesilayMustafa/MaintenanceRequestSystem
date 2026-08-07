using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed partial class TicketBehaviorTests
{
    private static Ticket CreateTicket()
    {
        return new Ticket(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Test talebi",
            "Test açıklaması",
            TicketPriority.Medium);
    }
}
