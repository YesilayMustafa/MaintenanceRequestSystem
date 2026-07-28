using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using System.Globalization;

namespace MaintenanceRequestSystem.UnitTests.Departments.Fakes;

internal sealed class FakeDepartmentRepository
    : IDepartmentRepository
{
    private readonly List<Department> _departments = new();

    public IReadOnlyList<Department> Items => _departments;

    public int SaveChangesCallCount { get; private set; }

    public void Seed(params Department[] departments)
    {
        _departments.AddRange(departments);
    }

    public Task<IReadOnlyList<Department>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<Department> result = _departments
            .OrderBy(department => department.Name)
            .ToList();

        return Task.FromResult(result);
    }

    public Task<Department?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var department = _departments
            .FirstOrDefault(department => department.Id == id);

        return Task.FromResult(department);
    }

    public Task<bool> ExistsByNameAsync(
    string name,
    Guid? excludedDepartmentId = null,
    CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedName = NormalizeName(name);

        var exists = _departments.Any(department =>
            (!excludedDepartmentId.HasValue ||
             department.Id != excludedDepartmentId.Value) &&
            NormalizeName(department.Name) == normalizedName);

        return Task.FromResult(exists);
    }

    public Task AddAsync(
        Department department,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _departments.Add(department);

        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SaveChangesCallCount++;

        return Task.FromResult(1);
    }

    private static string NormalizeName(string name)
    {
        return name
            .Trim()
            .ToUpper(CultureInfo.GetCultureInfo("tr-TR"));
    }
}