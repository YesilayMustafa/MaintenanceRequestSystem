using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Assets.Models;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Assets.Services;

public sealed class AssetMaintenanceHistoryService
    : IAssetMaintenanceHistoryService
{
    private readonly IAssetMaintenanceHistoryRepository _repository;

    public AssetMaintenanceHistoryService(
        IAssetMaintenanceHistoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<AssetMaintenanceHistoryDto> GetAsync(
        Guid assetId,
        Guid currentUserId,
        UserRole currentUserRole,
        AssetMaintenanceHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(assetId, "Geçerli bir cihaz kimliği gereklidir.");
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");

        if (!Enum.IsDefined(currentUserRole))
        {
            throw new ForbiddenException("Desteklenmeyen kullanıcı rolü.");
        }

        ArgumentNullException.ThrowIfNull(query);
        ValidateQuery(query);

        var result = await _repository.GetAsync(
            assetId,
            currentUserId,
            currentUserRole,
            query,
            cancellationToken);

        if (result is null)
        {
            throw new KeyNotFoundException("Cihaz bulunamadı.");
        }

        return MapToDto(result, query);
    }

    private static AssetMaintenanceHistoryDto MapToDto(
        AssetMaintenanceHistoryData data,
        AssetMaintenanceHistoryQuery query)
    {
        var totalPages = data.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(data.TotalCount / (double)query.PageSize);
        var items = data.Tickets.Select(ticket =>
            new AssetMaintenanceTicketDto(
                ticket.Id,
                ticket.TicketNumber,
                ticket.Title,
                ticket.CategoryId,
                ticket.CategoryName,
                ticket.Status.ToString(),
                ticket.Priority.ToString(),
                ticket.CreatedAt,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                ticket.CreatedByFullName,
                ticket.AssignedTechnicianFullName)).ToList();

        return new AssetMaintenanceHistoryDto(
            new AssetMaintenanceHistoryAssetDto(
                data.Asset.Id,
                data.Asset.Name,
                data.Asset.SerialNumber,
                data.Asset.Type.ToString()),
            new AssetMaintenanceSummaryDto(
                data.Summary.TotalTicketCount,
                data.Summary.ActiveTicketCount,
                data.Summary.ResolvedTicketCount,
                data.Summary.ClosedTicketCount,
                data.Summary.CriticalTicketCount,
                data.Summary.LastTicketCreatedAt),
            new PagedResult<AssetMaintenanceTicketDto>(
                items,
                query.PageNumber,
                query.PageSize,
                data.TotalCount,
                totalPages));
    }

    private static void ValidateQuery(AssetMaintenanceHistoryQuery query)
    {
        if (query.PageNumber < 1)
        {
            throw new RequestValidationException("Sayfa numarası en az 1 olmalıdır.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }

        if (((long)query.PageNumber - 1L) * query.PageSize > int.MaxValue)
        {
            throw new RequestValidationException(
                "İstenen sayfa numarası desteklenen sınırı aşıyor.");
        }
    }

    private static void EnsureValidId(Guid id, string message)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(message);
        }
    }
}
