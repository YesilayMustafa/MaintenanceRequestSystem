using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class AccountTokenRepository
    : IAccountTokenRepository
{
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim>
        InMemoryTokenLocks = new();

    private readonly ApplicationDbContext _context;

    public AccountTokenRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<AccountToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _context.AccountTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AccountToken>>
        GetActiveByUserAndTypeAsync(
            Guid userId,
            AccountTokenType type,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
    {
        return await _context.AccountTokens
            .Where(token =>
                token.UserId == userId &&
                token.Type == type &&
                token.UsedAt == null &&
                token.RevokedAt == null &&
                token.ExpiresAt > utcNow)
            .OrderBy(token => token.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(
        AccountToken accountToken,
        CancellationToken cancellationToken = default)
    {
        await _context.AccountTokens.AddAsync(
            accountToken,
            cancellationToken);
    }

    public async Task<bool> TryConsumeAsync(
        Guid tokenId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (_context.Database.IsRelational())
        {
            var affectedRows = await _context.AccountTokens
                .Where(token =>
                    token.Id == tokenId &&
                    token.UsedAt == null &&
                    token.RevokedAt == null &&
                    token.ExpiresAt > utcNow)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(
                        token => token.UsedAt,
                        utcNow),
                    cancellationToken);

            return affectedRows == 1;
        }

        var tokenLock = InMemoryTokenLocks.GetOrAdd(
            tokenId,
            _ => new SemaphoreSlim(1, 1));

        await tokenLock.WaitAsync(cancellationToken);

        try
        {
            var token = await _context.AccountTokens
                .SingleOrDefaultAsync(
                    accountToken => accountToken.Id == tokenId,
                    cancellationToken);

            if (token is null || !token.CanBeUsed(utcNow))
            {
                return false;
            }

            token.Consume(utcNow);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        finally
        {
            tokenLock.Release();
        }
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
