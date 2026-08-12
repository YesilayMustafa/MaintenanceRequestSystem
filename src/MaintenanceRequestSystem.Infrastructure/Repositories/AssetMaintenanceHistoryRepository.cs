using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Assets.Models;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class AssetMaintenanceHistoryRepository
    : IAssetMaintenanceHistoryRepository
{
    private readonly ApplicationDbContext _context;

    public AssetMaintenanceHistoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssetMaintenanceHistoryData?> GetAsync(
        Guid assetId,
        Guid currentUserId,
        UserRole currentUserRole,
        AssetMaintenanceHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        var asset = await _context.Assets
            .AsNoTracking()
            .Where(item => item.Id == assetId)
            .Select(item => new AssetMaintenanceAssetData(
                item.Id,
                item.Name,
                item.SerialNumber,
                item.Type))
            .FirstOrDefaultAsync(cancellationToken);

        if (asset is null)
        {
            return null;
        }

        var ticketQuery = TicketQueryScope.Apply(
                _context.Tickets.AsNoTracking(),
                currentUserId,
                currentUserRole)
            .Where(ticket => ticket.AssetId == assetId);

        var totalCount = await ticketQuery.CountAsync(cancellationToken);
        var activeCount = await ticketQuery.CountAsync(
            ticket => ticket.Status != TicketStatus.Resolved &&
                ticket.Status != TicketStatus.Closed &&
                ticket.Status != TicketStatus.Cancelled,
            cancellationToken);
        var resolvedCount = await ticketQuery.CountAsync(
            ticket => ticket.Status == TicketStatus.Resolved,
            cancellationToken);
        var closedCount = await ticketQuery.CountAsync(
            ticket => ticket.Status == TicketStatus.Closed,
            cancellationToken);
        var criticalCount = await ticketQuery.CountAsync(
            ticket => ticket.Priority == TicketPriority.Critical,
            cancellationToken);
        var lastTicketCreatedAt = await ticketQuery
            .Select(ticket => (DateTime?)ticket.CreatedAt)
            .MaxAsync(cancellationToken);

        var offset = ((long)query.PageNumber - 1L) * query.PageSize;
        var tickets = await ticketQuery
            .OrderByDescending(ticket => ticket.CreatedAt)
            .ThenBy(ticket => ticket.Id)
            .Skip(checked((int)offset))
            .Take(query.PageSize)
            .Select(ticket => new AssetMaintenanceTicketData(
                ticket.Id,
                ticket.TicketNumber,
                ticket.Title,
                ticket.CategoryId,
                ticket.Category.Name,
                ticket.Status,
                ticket.Priority,
                ticket.CreatedAt,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                ticket.CreatedByUser.FullName,
                ticket.AssignedTechnician == null
                    ? null
                    : ticket.AssignedTechnician.FullName))
            .ToListAsync(cancellationToken);

        return new AssetMaintenanceHistoryData(
            asset,
            new AssetMaintenanceSummaryData(
                totalCount,
                activeCount,
                resolvedCount,
                closedCount,
                criticalCount,
                lastTicketCreatedAt),
            tickets,
            totalCount);
    }
}
