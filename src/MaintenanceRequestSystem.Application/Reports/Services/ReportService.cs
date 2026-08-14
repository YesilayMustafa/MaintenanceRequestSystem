using System.Globalization;
using System.Text;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Reports.Dtos;
using MaintenanceRequestSystem.Application.Reports.Interfaces;
using MaintenanceRequestSystem.Application.Reports.Models;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Reports.Services;

public sealed class ReportService : IReportService
{
    private readonly IReportRepository _repository;
    private readonly TimeProvider _timeProvider;

    public ReportService(
        IReportRepository repository,
        TimeProvider? timeProvider = null)
    {
        _repository = repository;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task<ReportOverviewDto> GetOverviewAsync(
        UserRole currentUserRole,
        ReportFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUserRole);
        ValidateQuery(query);

        return _repository.GetOverviewAsync(
            query,
            _timeProvider.GetUtcNow().UtcDateTime,
            cancellationToken);
    }

    public async Task<ReportCsvFile> ExportTicketsAsync(
        UserRole currentUserRole,
        ReportFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAdmin(currentUserRole);
        ValidateQuery(query);
        var utcNow = _timeProvider.GetUtcNow().UtcDateTime;
        var rows = await _repository.GetTicketExportRowsAsync(
            query,
            utcNow,
            cancellationToken);
        var builder = new StringBuilder();
        builder.AppendLine("sep=;");
        builder.AppendLine(string.Join(';', new[]
        {
            "Talep No", "Başlık", "Kategori", "Durum", "Öncelik",
            "Açılış", "SLA Son", "SLA", "Oluşturan", "Departman",
            "Teknisyen"
        }.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(';', new[]
            {
                row.TicketNumber,
                row.Title,
                row.Category,
                row.Status,
                row.Priority,
                FormatUtcDate(row.CreatedAt),
                FormatUtcDate(row.SlaDueAt),
                FormatSlaStatus(row.SlaStatus),
                row.CreatedBy,
                row.Department,
                row.AssignedTechnician ?? string.Empty
            }.Select(EscapeCsv)));
        }

        var encoding = new UnicodeEncoding(
            bigEndian: false,
            byteOrderMark: true);
        var csvBytes = encoding.GetBytes(builder.ToString());
        var preamble = encoding.GetPreamble();
        var content = new byte[preamble.Length + csvBytes.Length];
        preamble.CopyTo(content, 0);
        csvBytes.CopyTo(content, preamble.Length);

        return new ReportCsvFile(
            content,
            "text/csv; charset=utf-16",
            $"ticket-report-{utcNow:yyyy-MM-dd}.csv");
    }

    private static string FormatUtcDate(DateTime value)
    {
        return value.ToString(
            "dd.MM.yyyy HH:mm 'UTC'",
            CultureInfo.InvariantCulture);
    }

    private static string FormatSlaStatus(string value)
    {
        return value switch
        {
            nameof(SlaStatus.OnTrack) => "Süre İçinde",
            nameof(SlaStatus.DueSoon) => "Süre Yaklaşıyor",
            nameof(SlaStatus.Breached) => "SLA Aşıldı",
            nameof(SlaStatus.Met) => "SLA Karşılandı",
            nameof(SlaStatus.NotApplicable) => "Uygulanamaz",
            _ => value
        };
    }

    private static string EscapeCsv(string value)
    {
        var safeValue = value;

        if (!string.IsNullOrEmpty(safeValue) &&
            safeValue[0] is '=' or '+' or '-' or '@')
        {
            safeValue = "'" + safeValue;
        }

        return $"\"{safeValue.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
    }

    private static void EnsureAdmin(UserRole role)
    {
        if (role != UserRole.Admin)
        {
            throw new ForbiddenException("Raporları yalnızca yöneticiler görüntüleyebilir.");
        }
    }

    private static void ValidateQuery(ReportFilterQuery query)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.CreatedFrom.HasValue && query.CreatedFrom.Value.Kind != DateTimeKind.Utc)
        {
            throw new RequestValidationException("Başlangıç tarihi UTC formatında olmalıdır.");
        }

        if (query.CreatedTo.HasValue && query.CreatedTo.Value.Kind != DateTimeKind.Utc)
        {
            throw new RequestValidationException("Bitiş tarihi UTC formatında olmalıdır.");
        }

        if (query.CreatedFrom > query.CreatedTo)
        {
            throw new RequestValidationException(
                "Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        if (query.CategoryId == Guid.Empty ||
            query.DepartmentId == Guid.Empty ||
            query.AssignedTechnicianId == Guid.Empty)
        {
            throw new RequestValidationException("Filtre kimliği boş olamaz.");
        }
    }
}
