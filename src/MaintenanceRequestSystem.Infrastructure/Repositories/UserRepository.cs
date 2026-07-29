using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<User>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(user => user.Department)
            .OrderBy(user => user.FullName)
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(user => user.Department)
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            email.Trim().ToLowerInvariant();

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email == normalizedEmail,
                cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(
        string email,
        Guid? excludedUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail =
            email.Trim().ToLowerInvariant();

        return await _context.Users
            .AnyAsync(
                user =>
                    user.Email == normalizedEmail &&
                    (!excludedUserId.HasValue ||
                     user.Id != excludedUserId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(
            user,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}