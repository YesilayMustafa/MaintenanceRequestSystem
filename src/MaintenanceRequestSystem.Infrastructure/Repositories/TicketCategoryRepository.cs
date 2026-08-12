using System.Data;
using MaintenanceRequestSystem.Application.Categories.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class TicketCategoryRepository : ITicketCategoryRepository
{
    private static readonly SemaphoreSlim InMemoryTransactionLock =
        new(1, 1);

    private readonly ApplicationDbContext _context;

    public TicketCategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TicketCategory>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TicketCategories
            .AsNoTracking()
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(category => category.IsActive);
        }

        return await query
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<TicketCategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return _context.TicketCategories.FirstOrDefaultAsync(
            category => category.Id == id,
            cancellationToken);
    }

    public Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludedCategoryId = null,
        CancellationToken cancellationToken = default)
    {
        return _context.TicketCategories.AnyAsync(
            category =>
                category.NormalizedName == normalizedName &&
                (!excludedCategoryId.HasValue ||
                 category.Id != excludedCategoryId.Value),
            cancellationToken);
    }

    public Task<int> CountActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.TicketCategories.CountAsync(
            category => category.IsActive,
            cancellationToken);
    }

    public async Task AddAsync(
        TicketCategory category,
        CancellationToken cancellationToken = default)
    {
        await _context.TicketCategories.AddAsync(
            category,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new ConflictException(
                "Aynı isimde bir kategori zaten bulunmaktadır.");
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (!_context.Database.IsRelational())
        {
            await InMemoryTransactionLock.WaitAsync(cancellationToken);

            try
            {
                await operation(cancellationToken);
                return;
            }
            finally
            {
                InMemoryTransactionLock.Release();
            }
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (PostgresException exception)
            when (exception.SqlState ==
                PostgresErrorCodes.SerializationFailure)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException(
                "Kategori işlemi eşzamanlı bir değişiklikle çakıştı.");
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.SerializationFailure
            })
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException(
                "Kategori işlemi eşzamanlı bir değişiklikle çakıştı.");
        }
    }
}
