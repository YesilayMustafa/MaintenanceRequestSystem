using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class TicketRepository : ITicketRepository
{
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
                .Include(ticket => ticket.CreatedByUser)
                .Include(ticket => ticket.AssignedTechnician);

        if (currentUserRole == UserRole.Employee)
        {
            ticketQuery =
                ticketQuery.Where(
                    ticket =>
                        ticket.CreatedByUserId == currentUserId);
        }

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

        var totalCount =
            await ticketQuery.CountAsync(
                cancellationToken);

        ticketQuery =
            ApplySorting(
                ticketQuery,
                query.SortBy,
                query.SortDescending);

        var skip =
    (int)(
        ((long)query.PageNumber - 1L) *
        query.PageSize);

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
            .Include(ticket => ticket.CreatedByUser)
            .Include(ticket => ticket.AssignedTechnician)
            .FirstOrDefaultAsync(
                ticket => ticket.Id == id,
                cancellationToken);
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
}