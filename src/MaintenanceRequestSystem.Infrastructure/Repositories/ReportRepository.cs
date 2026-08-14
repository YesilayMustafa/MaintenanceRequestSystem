using MaintenanceRequestSystem.Application.Reports.Dtos;
using MaintenanceRequestSystem.Application.Reports.Interfaces;
using MaintenanceRequestSystem.Application.Reports.Models;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class ReportRepository : IReportRepository
{
    private readonly ApplicationDbContext _context;

    public ReportRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReportOverviewDto> GetOverviewAsync(
        ReportFilterQuery query,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var tickets = ApplyFilters(
            _context.Tickets.AsNoTracking(),
            query);
        var counts = await tickets
            .GroupBy(_ => 1)
            .Select(group => new ReportCounts(
                group.Count(),
                group.Count(ticket =>
                    ticket.Status != TicketStatus.Resolved &&
                    ticket.Status != TicketStatus.Closed &&
                    ticket.Status != TicketStatus.Cancelled),
                group.Count(ticket => ticket.Status == TicketStatus.Resolved),
                group.Count(ticket => ticket.Status == TicketStatus.Closed),
                group.Count(ticket => ticket.Status == TicketStatus.Cancelled),
                group.Count(ticket => ticket.Priority == TicketPriority.Critical),
                group.Count(ticket =>
                    (ticket.Status == TicketStatus.Resolved ||
                     ticket.Status == TicketStatus.Closed) &&
                    (ticket.ResolvedAt ?? ticket.ClosedAt) <= ticket.SlaDueAt),
                group.Count(ticket =>
                    ticket.Status != TicketStatus.Cancelled &&
                    (((ticket.Status == TicketStatus.Resolved ||
                       ticket.Status == TicketStatus.Closed) &&
                      (ticket.ResolvedAt ?? ticket.ClosedAt) > ticket.SlaDueAt) ||
                     (ticket.Status != TicketStatus.Resolved &&
                      ticket.Status != TicketStatus.Closed &&
                      ticket.SlaDueAt < utcNow)))))
            .SingleOrDefaultAsync(cancellationToken)
            ?? ReportCounts.Empty;

        var statusRows = await tickets
            .GroupBy(ticket => ticket.Status)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);
        var priorityRows = await tickets
            .GroupBy(ticket => ticket.Priority)
            .Select(group => new { Key = group.Key, Count = group.Count() })
            .OrderBy(item => item.Key)
            .ToListAsync(cancellationToken);
        var categoryRows = await tickets
            .GroupBy(ticket => new { ticket.CategoryId, ticket.Category.Name })
            .Select(group => new
            {
                group.Key.CategoryId,
                group.Key.Name,
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ThenBy(item => item.Name)
            .ToListAsync(cancellationToken);
        var trendRows = await tickets
            .GroupBy(ticket => ticket.CreatedAt.Date)
            .Select(group => new { Date = group.Key, Count = group.Count() })
            .OrderBy(item => item.Date)
            .ToListAsync(cancellationToken);
        var technicianRows = await tickets
            .Where(ticket => ticket.AssignedTechnicianId != null)
            .GroupBy(ticket => new
            {
                TechnicianId = ticket.AssignedTechnicianId!.Value,
                ticket.AssignedTechnician!.FullName
            })
            .Select(group => new
            {
                group.Key.TechnicianId,
                group.Key.FullName,
                AssignedCount = group.Count(),
                ActiveCount = group.Count(ticket =>
                    ticket.Status != TicketStatus.Resolved &&
                    ticket.Status != TicketStatus.Closed &&
                    ticket.Status != TicketStatus.Cancelled),
                ResolvedOrClosedCount = group.Count(ticket =>
                    ticket.Status == TicketStatus.Resolved ||
                    ticket.Status == TicketStatus.Closed),
                SlaMetCount = group.Count(ticket =>
                    (ticket.Status == TicketStatus.Resolved ||
                     ticket.Status == TicketStatus.Closed) &&
                    (ticket.ResolvedAt ?? ticket.ClosedAt) <= ticket.SlaDueAt),
                SlaBreachedCount = group.Count(ticket =>
                    ticket.Status != TicketStatus.Cancelled &&
                    (((ticket.Status == TicketStatus.Resolved ||
                       ticket.Status == TicketStatus.Closed) &&
                      (ticket.ResolvedAt ?? ticket.ClosedAt) > ticket.SlaDueAt) ||
                     (ticket.Status != TicketStatus.Resolved &&
                      ticket.Status != TicketStatus.Closed &&
                      ticket.SlaDueAt < utcNow)))
            })
            .OrderBy(item => item.FullName)
            .ThenBy(item => item.TechnicianId)
            .ToListAsync(cancellationToken);

        return new ReportOverviewDto(
            new ReportSummaryDto(
                counts.TotalTickets,
                counts.ActiveTickets,
                counts.ResolvedTickets,
                counts.ClosedTickets,
                counts.CancelledTickets,
                counts.CriticalTickets,
                counts.SlaMetCount,
                counts.SlaBreachedCount,
                GetComplianceRate(counts.SlaMetCount, counts.SlaBreachedCount)),
            statusRows.Select(item => new ReportDistributionItemDto(
                item.Key.ToString(), item.Key.ToString(), item.Count)).ToList(),
            priorityRows.Select(item => new ReportDistributionItemDto(
                item.Key.ToString(), item.Key.ToString(), item.Count)).ToList(),
            categoryRows.Select(item => new ReportDistributionItemDto(
                item.CategoryId.ToString(), item.Name, item.Count)).ToList(),
            trendRows.Select(item => new ReportTrendItemDto(
                DateOnly.FromDateTime(item.Date), item.Count)).ToList(),
            technicianRows.Select(item => new TechnicianPerformanceDto(
                item.TechnicianId,
                item.FullName,
                item.AssignedCount,
                item.ActiveCount,
                item.ResolvedOrClosedCount,
                item.SlaMetCount,
                item.SlaBreachedCount,
                GetComplianceRate(item.SlaMetCount, item.SlaBreachedCount)))
                .ToList());
    }

    public async Task<IReadOnlyList<TicketReportExportRow>>
        GetTicketExportRowsAsync(
            ReportFilterQuery query,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
    {
        var rows = await ApplyFilters(_context.Tickets.AsNoTracking(), query)
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenBy(ticket => ticket.Id)
            .Select(ticket => new TicketReportExportData(
                ticket.TicketNumber,
                ticket.Title,
                ticket.Category.Name,
                ticket.Status,
                ticket.Priority,
                ticket.CreatedAt,
                ticket.SlaDueAt,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                ticket.CreatedByUser.FullName,
                ticket.CreatedByUser.Department.Name,
                ticket.AssignedTechnician == null
                    ? null
                    : ticket.AssignedTechnician.FullName))
            .ToListAsync(cancellationToken);

        return rows.Select(row => new TicketReportExportRow(
            row.TicketNumber,
            row.Title,
            row.Category,
            row.Status.ToString(),
            row.Priority.ToString(),
            row.CreatedAt,
            row.SlaDueAt,
            GetSlaStatus(row, utcNow).ToString(),
            row.CreatedBy,
            row.Department,
            row.AssignedTechnician)).ToList();
    }

    private static IQueryable<Ticket> ApplyFilters(
        IQueryable<Ticket> query,
        ReportFilterQuery filter)
    {
        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(ticket => ticket.CreatedAt >= filter.CreatedFrom.Value);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(ticket => ticket.CreatedAt <= filter.CreatedTo.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(ticket => ticket.CategoryId == filter.CategoryId.Value);
        }

        if (filter.DepartmentId.HasValue)
        {
            query = query.Where(ticket =>
                ticket.CreatedByUser.DepartmentId == filter.DepartmentId.Value);
        }

        if (filter.AssignedTechnicianId.HasValue)
        {
            query = query.Where(ticket =>
                ticket.AssignedTechnicianId == filter.AssignedTechnicianId.Value);
        }

        return query;
    }

    private static decimal GetComplianceRate(int met, int breached)
    {
        var denominator = met + breached;
        return denominator == 0
            ? 0m
            : Math.Round(met * 100m / denominator, 2);
    }

    private static SlaStatus GetSlaStatus(
        TicketReportExportData row,
        DateTime utcNow)
    {
        if (row.Status == TicketStatus.Cancelled)
        {
            return SlaStatus.NotApplicable;
        }

        if (row.Status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            return (row.ResolvedAt ?? row.ClosedAt) <= row.SlaDueAt
                ? SlaStatus.Met
                : SlaStatus.Breached;
        }

        if (row.SlaDueAt < utcNow)
        {
            return SlaStatus.Breached;
        }

        var dueSoonThreshold = row.SlaDueAt - TimeSpan.FromTicks(
            (long)((row.SlaDueAt - row.CreatedAt).Ticks * 0.2));

        return utcNow >= dueSoonThreshold
            ? SlaStatus.DueSoon
            : SlaStatus.OnTrack;
    }

    private sealed record ReportCounts(
        int TotalTickets,
        int ActiveTickets,
        int ResolvedTickets,
        int ClosedTickets,
        int CancelledTickets,
        int CriticalTickets,
        int SlaMetCount,
        int SlaBreachedCount)
    {
        internal static ReportCounts Empty { get; } = new(0, 0, 0, 0, 0, 0, 0, 0);
    }

    private sealed record TicketReportExportData(
        string TicketNumber,
        string Title,
        string Category,
        TicketStatus Status,
        TicketPriority Priority,
        DateTime CreatedAt,
        DateTime SlaDueAt,
        DateTime? ResolvedAt,
        DateTime? ClosedAt,
        string CreatedBy,
        string Department,
        string? AssignedTechnician);
}
