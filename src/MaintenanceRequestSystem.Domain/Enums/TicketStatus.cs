using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Domain.Enums;

public enum TicketStatus
{
    Open = 1,
    Assigned = 2,
    InProgress = 3,
    Waiting = 4,
    Resolved = 5,
    Closed = 6,
    Cancelled = 7
}