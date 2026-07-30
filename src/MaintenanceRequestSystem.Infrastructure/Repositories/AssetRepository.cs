using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class AssetRepository : IAssetRepository
{
    private readonly ApplicationDbContext _context;

    public AssetRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Asset>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .AsNoTracking()
            .Include(asset => asset.Department)
            .OrderBy(asset => asset.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Asset?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Assets
            .Include(asset => asset.Department)
            .FirstOrDefaultAsync(
                asset => asset.Id == id,
                cancellationToken);
    }

    public async Task<bool> SerialNumberExistsAsync(
        string serialNumber,
        Guid? excludedAssetId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedSerialNumber =
            serialNumber.Trim().ToUpperInvariant();

        return await _context.Assets
            .AnyAsync(
                asset =>
                    asset.SerialNumber ==
                    normalizedSerialNumber &&
                    (!excludedAssetId.HasValue ||
                     asset.Id != excludedAssetId.Value),
                cancellationToken);
    }

    public async Task AddAsync(
        Asset asset,
        CancellationToken cancellationToken = default)
    {
        await _context.Assets.AddAsync(
            asset,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            throw new ConflictException(
                "Bu seri numarasıyla kayıtlı bir cihaz zaten var.");
        }
    }
}