using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Application.Dashboard.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class DashboardRepository : IDashboardRepository
{
    private const int RecentTicketLimit = 5;

    private static readonly TicketStatus[] ActiveStatuses =
    [
        TicketStatus.Open,
        TicketStatus.Assigned,
        TicketStatus.InProgress,
        TicketStatus.Waiting,
        TicketStatus.Resolved
    ];

    private static readonly TicketStatus[] WorkloadStatuses =
    [
        TicketStatus.Assigned,
        TicketStatus.InProgress,
        TicketStatus.Waiting
    ];

    private readonly ApplicationDbContext _context;

    public DashboardRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardDto> GetAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        var scopedTickets = TicketQueryScope.Apply(
            _context.Tickets.AsNoTracking(),
            currentUserId,
            currentUserRole);

        var utcNow = DateTime.UtcNow;

        var counts =
            await scopedTickets
                .GroupBy(_ => 1)
                .Select(group => new DashboardCounts(
                    group.Count(),
                    group.Count(ticket =>
                        ActiveStatuses.Contains(ticket.Status)),
                    group.Count(ticket => ticket.Status == TicketStatus.Open),
                    group.Count(ticket => ticket.Status == TicketStatus.Assigned),
                    group.Count(ticket => ticket.Status == TicketStatus.InProgress),
                    group.Count(ticket => ticket.Status == TicketStatus.Waiting),
                    group.Count(ticket => ticket.Status == TicketStatus.Resolved),
                    group.Count(ticket => ticket.Status == TicketStatus.Closed),
                    group.Count(ticket => ticket.Status == TicketStatus.Cancelled),
                    group.Count(ticket =>
                        ticket.Priority == TicketPriority.Critical &&
                        ActiveStatuses.Contains(ticket.Status)),
                    group.Count(ticket =>
                        ticket.Status != TicketStatus.Cancelled &&
                        (((ticket.Status == TicketStatus.Resolved ||
                           ticket.Status == TicketStatus.Closed) &&
                          (ticket.ResolvedAt ?? ticket.ClosedAt) > ticket.SlaDueAt) ||
                         (ticket.Status != TicketStatus.Resolved &&
                          ticket.Status != TicketStatus.Closed &&
                          ticket.SlaDueAt < utcNow))),
                    group.Count(ticket =>
                        ticket.Status != TicketStatus.Resolved &&
                        ticket.Status != TicketStatus.Closed &&
                        ticket.Status != TicketStatus.Cancelled &&
                        ticket.SlaDueAt >= utcNow &&
                        utcNow >= ticket.SlaDueAt -
                            (ticket.SlaDueAt - ticket.CreatedAt) * 0.2)))
                .SingleOrDefaultAsync(cancellationToken)
            ?? DashboardCounts.Empty;

        var recentTicketRows =
            await scopedTickets
                .OrderByDescending(ticket => ticket.CreatedAt)
                .ThenBy(ticket => ticket.Id)
                .Take(RecentTicketLimit)
                .Select(ticket => new DashboardTicketRow(
                    ticket.Id,
                    ticket.TicketNumber,
                    ticket.Title,
                    ticket.Status,
                    ticket.Priority,
                    ticket.Asset.Name,
                    ticket.CreatedAt,
                    ticket.AssignedTechnician == null
                        ? null
                        : ticket.AssignedTechnician.FullName))
                .ToListAsync(cancellationToken);

        var recentTickets = recentTicketRows
            .Select(ticket => new DashboardTicketDto(
                ticket.Id,
                ticket.TicketNumber,
                ticket.Title,
                ticket.Status.ToString(),
                ticket.Priority.ToString(),
                ticket.AssetName,
                ticket.CreatedAt,
                ticket.AssignedTechnicianFullName))
            .ToList();

        var admin = currentUserRole == UserRole.Admin
            ? await GetAdminDashboardAsync(cancellationToken)
            : null;

        return new DashboardDto(
            counts.TotalCount,
            counts.ActiveCount,
            counts.OpenCount,
            counts.AssignedCount,
            counts.InProgressCount,
            counts.WaitingCount,
            counts.ResolvedCount,
            counts.ClosedCount,
            counts.CancelledCount,
            counts.CriticalActiveCount,
            recentTickets,
            admin,
            counts.SlaBreachedCount,
            counts.SlaDueSoonCount);
    }

    private async Task<AdminDashboardDto> GetAdminDashboardAsync(
        CancellationToken cancellationToken)
    {
        var unassignedOpenCount =
            await _context.Tickets
                .AsNoTracking()
                .CountAsync(ticket =>
                    ticket.Status == TicketStatus.Open &&
                    ticket.AssignedTechnicianId == null,
                    cancellationToken);

        var technicianWorkload =
            await _context.Users
                .AsNoTracking()
                .Where(user =>
                    user.Role == UserRole.Technician &&
                    user.IsActive &&
                    user.InvitationAcceptedAt != null &&
                    user.PasswordHash != null &&
                    user.PasswordHash != string.Empty)
                .OrderBy(user => user.FullName)
                .ThenBy(user => user.Id)
                .Select(user => new TechnicianWorkloadDto(
                    user.Id,
                    user.FullName,
                    _context.Tickets.Count(ticket =>
                        ticket.AssignedTechnicianId == user.Id &&
                        WorkloadStatuses.Contains(ticket.Status))))
                .ToListAsync(cancellationToken);

        return new AdminDashboardDto(
            unassignedOpenCount,
            technicianWorkload);
    }

    private sealed record DashboardCounts(
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
        int SlaBreachedCount,
        int SlaDueSoonCount)
    {
        internal static DashboardCounts Empty { get; } =
            new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
    }

    private sealed record DashboardTicketRow(
        Guid Id,
        string TicketNumber,
        string Title,
        TicketStatus Status,
        TicketPriority Priority,
        string AssetName,
        DateTime CreatedAt,
        string? AssignedTechnicianFullName);
}
