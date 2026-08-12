using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Categories.Interfaces;

public interface ITicketCategoryRepository
{
    Task<IReadOnlyList<TicketCategory>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task<TicketCategory?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(
        string normalizedName,
        Guid? excludedCategoryId = null,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveAsync(
        CancellationToken cancellationToken = default);

    Task AddAsync(
        TicketCategory category,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        return operation(cancellationToken);
    }
}
