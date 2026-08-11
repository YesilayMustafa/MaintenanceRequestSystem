using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IAccountTokenRepository
{
    Task<AccountToken?> GetByHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountToken>> GetActiveByUserAndTypeAsync(
        Guid userId,
        AccountTokenType type,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        AccountToken accountToken,
        CancellationToken cancellationToken = default);

    Task<bool> TryConsumeAsync(
        Guid tokenId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
