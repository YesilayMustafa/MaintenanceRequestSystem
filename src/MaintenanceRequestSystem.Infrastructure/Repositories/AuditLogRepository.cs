using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

/// <summary>
/// Audit kayıtlarının EF Core üzerinden PostgreSQL'e
/// kaydedilmesini sağlar.
/// </summary>
public sealed class AuditLogRepository
    : IAuditLogRepository
{
    private readonly ApplicationDbContext _context;

    public AuditLogRepository(
        ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Audit kayıtlarını filtrelenmiş ve sayfalanmış olarak getirir.
    /// </summary>
    public async Task<(
        IReadOnlyList<AuditLog> Items,
        int TotalCount)> GetPagedAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        IQueryable<AuditLog> auditLogQuery =
            _context.AuditLogs
                .AsNoTracking()
                .Include(auditLog =>
                    auditLog.PerformedByUser);

        if (query.PerformedByUserId.HasValue)
        {
            auditLogQuery =
                auditLogQuery.Where(
                    auditLog =>
                        auditLog.PerformedByUserId ==
                        query.PerformedByUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(
                query.Action))
        {
            var action =
                query.Action.Trim();

            auditLogQuery =
                auditLogQuery.Where(
                    auditLog =>
                        auditLog.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(
                query.EntityName))
        {
            var entityName =
                query.EntityName.Trim();

            auditLogQuery =
                auditLogQuery.Where(
                    auditLog =>
                        auditLog.EntityName == entityName);
        }

        if (!string.IsNullOrWhiteSpace(
                query.EntityId))
        {
            var entityId =
                query.EntityId.Trim();

            auditLogQuery =
                auditLogQuery.Where(
                    auditLog =>
                        auditLog.EntityId == entityId);
        }

        if (query.StartDate.HasValue)
        {
            auditLogQuery =
                auditLogQuery.Where(
                    auditLog =>
                        auditLog.CreatedAt >=
                        query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            auditLogQuery =
                auditLogQuery.Where(
                    auditLog =>
                        auditLog.CreatedAt <=
                        query.EndDate.Value);
        }

        var totalCount =
            await auditLogQuery.CountAsync(
                cancellationToken);

        var offset =
            ((long)query.PageNumber - 1L) *
            query.PageSize;

        var skip =
            checked((int)offset);

        var items =
            await auditLogQuery
                .OrderByDescending(
                    auditLog =>
                        auditLog.CreatedAt)
                .ThenBy(
                    auditLog =>
                        auditLog.Id)
                .Skip(skip)
                .Take(query.PageSize)
                .ToListAsync(
                    cancellationToken);

        return (
            items,
            totalCount);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        AuditLog auditLog,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditLog);

        await _context.AuditLogs.AddAsync(
            auditLog,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(
            cancellationToken);
    }
}
