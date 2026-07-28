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
}