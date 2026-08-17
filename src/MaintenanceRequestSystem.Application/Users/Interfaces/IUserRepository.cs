using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Users.Interfaces;

public interface IUserRepository
{
    Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<Guid>> GetOperationalUserIdsByRoleAsync(
        UserRole role,
        CancellationToken cancellationToken = default)
    {
        var users = await GetAllAsync(cancellationToken);

        return users
            .Where(user => user.Role == role && user.IsOperational)
            .Select(user => user.Id)
            .ToList();
    }

    Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        User user,
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
