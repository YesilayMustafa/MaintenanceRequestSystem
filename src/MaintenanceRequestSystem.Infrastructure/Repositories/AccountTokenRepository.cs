using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class AccountTokenRepository
    : IAccountTokenRepository
{
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

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
