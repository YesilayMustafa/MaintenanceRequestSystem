using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
namespace MaintenanceRequestSystem.Infrastructure.Repositories;
public sealed class TicketRepository : ITicketRepository
{
    private const string InMemoryProvider =
        "Microsoft.EntityFrameworkCore.InMemory";

    private readonly ApplicationDbContext _context;

    public TicketRepository(ApplicationDbContext context)
    {
        _context = context;
    }
    public async Task<(IReadOnlyList<Ticket> Items, int TotalCount)>
    GetPagedAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        TicketListQuery query,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Ticket> ticketQuery =
            _context.Tickets
                .AsNoTracking()
                .Include(ticket => ticket.Asset)
                .Include(ticket => ticket.Category)
                .Include(ticket => ticket.CreatedByUser)
                .Include(ticket => ticket.AssignedTechnician);

        ticketQuery = TicketQueryScope.Apply(
            ticketQuery,
            currentUserId,
            currentUserRole);

        if (query.Status.HasValue)
        {
            ticketQuery =
                ticketQuery.Where(
                    ticket =>
                        ticket.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            ticketQuery =
                ticketQuery.Where(
                    ticket =>
                        ticket.Priority == query.Priority.Value);
        }

        if (query.AssetId.HasValue)
        {
            ticketQuery =
                ticketQuery.Where(
                    ticket =>
                        ticket.AssetId == query.AssetId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.TicketNumber))
        {
            var ticketNumberPrefix =
                query.TicketNumber.Trim().ToUpperInvariant();

            ticketQuery =
                ticketQuery.Where(ticket =>
                    ticket.TicketNumber.StartsWith(ticketNumberPrefix));
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();

            if (string.Equals(
                    _context.Database.ProviderName,
                    InMemoryProvider,
                    StringComparison.Ordinal))
            {
                ticketQuery = ticketQuery.Where(ticket =>
                    ticket.TicketNumber.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    ticket.Title.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase) ||
                    ticket.Description.Contains(
                        search,
                        StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                var pattern = $"%{EscapeLikePattern(search)}%";

                ticketQuery = ticketQuery.Where(ticket =>
                    EF.Functions.ILike(
                        ticket.TicketNumber,
                        pattern,
                        "\\") ||
                    EF.Functions.ILike(
                        ticket.Title,
                        pattern,
                        "\\") ||
                    EF.Functions.ILike(
                        ticket.Description,
                        pattern,
                        "\\"));
            }
        }

        if (query.CategoryId.HasValue)
        {
            ticketQuery = ticketQuery.Where(ticket =>
                ticket.CategoryId == query.CategoryId.Value);
        }

        if (query.CreatedByUserId.HasValue)
        {
            ticketQuery = ticketQuery.Where(ticket =>
                ticket.CreatedByUserId == query.CreatedByUserId.Value);
        }

        if (query.AssignedTechnicianId.HasValue)
        {
            ticketQuery = ticketQuery.Where(ticket =>
                ticket.AssignedTechnicianId ==
                query.AssignedTechnicianId.Value);
        }

        if (query.DepartmentId.HasValue)
        {
            ticketQuery = ticketQuery.Where(ticket =>
                ticket.CreatedByUser.DepartmentId ==
                query.DepartmentId.Value);
        }

        if (query.CreatedFrom.HasValue)
        {
            ticketQuery = ticketQuery.Where(ticket =>
                ticket.CreatedAt >= query.CreatedFrom.Value);
        }

        if (query.CreatedTo.HasValue)
        {
            ticketQuery = ticketQuery.Where(ticket =>
                ticket.CreatedAt <= query.CreatedTo.Value);
        }

        ticketQuery =
            ApplySorting(
                ticketQuery,
                query.SortBy,
                query.SortDescending);

        var totalCount =
            await ticketQuery.CountAsync(
                cancellationToken);

        var offset =
            ((long)query.PageNumber - 1L) *
            query.PageSize;

        var skip =
            checked((int)offset);

        var items =
            await ticketQuery
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Ticket?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .Include(ticket => ticket.Asset)
            .Include(ticket => ticket.Category)
            .Include(ticket => ticket.CreatedByUser)
            .Include(ticket => ticket.AssignedTechnician)
            .FirstOrDefaultAsync(
                ticket => ticket.Id == id,
                cancellationToken);
    }

    /// <summary>
    /// Belirtilen talebe ait durum geçmişini kronolojik olarak getirir.
    /// </summary>
    public async Task<IReadOnlyList<TicketHistory>> GetHistoriesAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        return await _context
            .Set<TicketHistory>()
            .AsNoTracking()
            .Where(history =>
                history.TicketId == ticketId)
            .OrderBy(history =>
                history.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        Ticket ticket,
        CancellationToken cancellationToken = default)
    {
        await _context.Tickets.AddAsync(
            ticket,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
    private static IQueryable<Ticket> ApplySorting(
    IQueryable<Ticket> query,
    string sortBy,
    bool sortDescending)
    {
        var normalizedSortBy =
            sortBy.Trim().ToLowerInvariant();

        return normalizedSortBy switch
        {
            "ticketnumber" when sortDescending =>
                query.OrderByDescending(ticket => ticket.TicketNumber)
                    .ThenBy(ticket => ticket.Id),

            "ticketnumber" =>
                query.OrderBy(ticket => ticket.TicketNumber)
                    .ThenBy(ticket => ticket.Id),

            "category" when sortDescending =>
                query.OrderByDescending(ticket => ticket.Category.Name)
                    .ThenBy(ticket => ticket.Id),

            "category" =>
                query.OrderBy(ticket => ticket.Category.Name)
                    .ThenBy(ticket => ticket.Id),

            "title" when sortDescending =>
                query.OrderByDescending(
                    ticket => ticket.Title)
                .ThenBy(ticket => ticket.Id),

            "title" =>
                query.OrderBy(
                    ticket => ticket.Title)
                .ThenBy(ticket => ticket.Id),

            "priority" when sortDescending =>
                query.OrderByDescending(
                    ticket =>
                        ticket.Priority ==
                        TicketPriority.Critical ? 4 :
                        ticket.Priority ==
                        TicketPriority.High ? 3 :
                        ticket.Priority ==
                        TicketPriority.Medium ? 2 : 1)
                .ThenBy(ticket => ticket.Id),

            "priority" =>
                query.OrderBy(
                    ticket =>
                        ticket.Priority ==
                        TicketPriority.Critical ? 4 :
                        ticket.Priority ==
                        TicketPriority.High ? 3 :
                        ticket.Priority ==
                        TicketPriority.Medium ? 2 : 1)
                .ThenBy(ticket => ticket.Id),

            "status" when sortDescending =>
                query.OrderByDescending(
                    ticket =>
                        ticket.Status ==
                        TicketStatus.Cancelled ? 7 :
                        ticket.Status ==
                        TicketStatus.Closed ? 6 :
                        ticket.Status ==
                        TicketStatus.Resolved ? 5 :
                        ticket.Status ==
                        TicketStatus.Waiting ? 4 :
                        ticket.Status ==
                        TicketStatus.InProgress ? 3 :
                        ticket.Status ==
                        TicketStatus.Assigned ? 2 : 1)
                .ThenBy(ticket => ticket.Id),

            "status" =>
                query.OrderBy(
                    ticket =>
                        ticket.Status ==
                        TicketStatus.Cancelled ? 7 :
                        ticket.Status ==
                        TicketStatus.Closed ? 6 :
                        ticket.Status ==
                        TicketStatus.Resolved ? 5 :
                        ticket.Status ==
                        TicketStatus.Waiting ? 4 :
                        ticket.Status ==
                        TicketStatus.InProgress ? 3 :
                        ticket.Status ==
                        TicketStatus.Assigned ? 2 : 1)
                .ThenBy(ticket => ticket.Id),

            _ when sortDescending =>
                query.OrderByDescending(
                    ticket => ticket.CreatedAt)
                .ThenBy(ticket => ticket.Id),

            _ =>
                query.OrderBy(
                    ticket => ticket.CreatedAt)
                .ThenBy(ticket => ticket.Id)
        };
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
    }
}
