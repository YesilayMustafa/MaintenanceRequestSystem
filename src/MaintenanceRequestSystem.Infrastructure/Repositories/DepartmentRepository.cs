using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly ApplicationDbContext _context;

    public DepartmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Department>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .AsNoTracking()
            .OrderBy(department => department.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Department?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(
                department => department.Id == id,
                cancellationToken);
    }

    public async Task<bool> ExistsByNameAsync(
    string name,
    Guid? excludedDepartmentId = null,
    CancellationToken cancellationToken = default)
    {
        var query = _context.Departments
            .AsNoTracking()
            .AsQueryable();

        if (excludedDepartmentId.HasValue)
        {
            query = query.Where(department =>
                department.Id != excludedDepartmentId.Value);
        }

        var existingNames = await query
            .Select(department => department.Name)
            .ToListAsync(cancellationToken);

        var normalizedName = NormalizeName(name);

        return existingNames.Any(existingName =>
            NormalizeName(existingName) == normalizedName);
    }

    public async Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default)
    {
        await _context.Departments.AddAsync(
            department,
            cancellationToken);
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
    private static string NormalizeName(string name)
    {
        return name
            .Trim()
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
    }
}