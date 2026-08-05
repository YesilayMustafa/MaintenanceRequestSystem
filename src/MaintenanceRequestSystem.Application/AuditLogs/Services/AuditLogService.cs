using System.Text.Json;
using System.Text.Json.Serialization;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.AuditLogs.Services;

namespace MaintenanceRequestSystem.Application.AuditLogs.Services;

/// <summary>
/// Audit kayıtlarını oluşturur ve repository üzerinden
/// mevcut EF çalışma birimine ekler.
/// </summary>
public sealed class AuditLogService
    : IAuditLogService
{


    private static readonly JsonSerializerOptions JsonOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,

            Converters =
            {
                new JsonStringEnumConverter()
            }
        };

    private readonly IAuditLogRepository _auditLogRepository;

    public AuditLogService(
        IAuditLogRepository auditLogRepository)
    {
        _auditLogRepository =
            auditLogRepository;
    }

    /// <summary>
    /// Audit kayıtlarını filtrelenmiş ve sayfalanmış olarak getirir.
    /// </summary>
    public async Task<PagedResult<AuditLogDto>> GetPagedAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        ValidateListQuery(query);

        var result =
            await _auditLogRepository.GetPagedAsync(
                query,
                cancellationToken);

        var items =
            result.Items
                .Select(MapToDto)
                .ToList();

        var totalPages =
            result.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalCount /
                    (double)query.PageSize);

        return new PagedResult<AuditLogDto>(
            items,
            query.PageNumber,
            query.PageSize,
            result.TotalCount,
            totalPages);
    }

    /// <inheritdoc />
    public async Task AddAsync(
        Guid performedByUserId,
        string action,
        string entityName,
        string entityId,
        object? oldValues = null,
        object? newValues = null,
        CancellationToken cancellationToken = default)
    {
        var auditLog =
            new AuditLog(
                performedByUserId,
                action,
                entityName,
                entityId,
                Serialize(oldValues),
                Serialize(newValues));

        await _auditLogRepository.AddAsync(
            auditLog,
            cancellationToken);
    }

    private static string? Serialize(
        object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(
            value,
            JsonOptions);
    }

    /// <summary>
    /// Audit listeleme sorgusunun sayfalama ve filtre
    /// kurallarını doğrular.
    /// </summary>
    private static void ValidateListQuery(
        AuditLogListQuery query)
    {
        if (query.PageNumber < 1)
        {
            throw new RequestValidationException(
                "Sayfa numarası en az 1 olmalıdır.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }

        var offset =
            ((long)query.PageNumber - 1L) *
            query.PageSize;

        if (offset > int.MaxValue)
        {
            throw new RequestValidationException(
                "İstenen sayfa numarası desteklenen sınırı aşıyor.");
        }

        if (query.PerformedByUserId == Guid.Empty)
        {
            throw new RequestValidationException(
                "İşlemi yapan kullanıcı kimliği boş olamaz.");
        }
        if (query.StartDate.HasValue &&
    query.StartDate.Value.Kind != DateTimeKind.Utc)
        {
            throw new RequestValidationException(
                "Başlangıç tarihi UTC formatında olmalıdır.");
        }

        if (query.EndDate.HasValue &&
            query.EndDate.Value.Kind != DateTimeKind.Utc)
        {
            throw new RequestValidationException(
                "Bitiş tarihi UTC formatında olmalıdır.");
        }

        if (query.StartDate.HasValue &&
            query.EndDate.HasValue &&
            query.StartDate.Value >
            query.EndDate.Value)
        {
            throw new RequestValidationException(
                "Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }
    }

    /// <summary>
    /// AuditLog entity nesnesini API yanıt modeline dönüştürür.
    /// </summary>
    private static AuditLogDto MapToDto(
        AuditLog auditLog)
    {
        return new AuditLogDto
        {
            Id = auditLog.Id,
            PerformedByUserId =
                auditLog.PerformedByUserId,
            PerformedByUserFullName =
                auditLog.PerformedByUser?.FullName
                ?? string.Empty,
            Action = auditLog.Action,
            EntityName = auditLog.EntityName,
            EntityId = auditLog.EntityId,
            OldValues = auditLog.OldValues,
            NewValues = auditLog.NewValues,
            CreatedAt = auditLog.CreatedAt
        };
    }
}
