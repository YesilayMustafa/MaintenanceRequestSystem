namespace MaintenanceRequestSystem.Application.Dashboard.Dtos;

public sealed record DashboardDto(
    int TotalCount,
    int ActiveCount,
    int OpenCount,
    int AssignedCount,
    int InProgressCount,
    int WaitingCount,
    int ResolvedCount,
    int ClosedCount,
    int CancelledCount,
    int CriticalActiveCount,
    IReadOnlyList<DashboardTicketDto> RecentTickets,
    AdminDashboardDto? Admin,
    int SlaBreachedCount = 0,
    int SlaDueSoonCount = 0);
