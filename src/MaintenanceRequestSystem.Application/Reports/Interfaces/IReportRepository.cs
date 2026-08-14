using MaintenanceRequestSystem.Application.Reports.Dtos;
using MaintenanceRequestSystem.Application.Reports.Models;

namespace MaintenanceRequestSystem.Application.Reports.Interfaces;

public interface IReportRepository
{
    Task<ReportOverviewDto> GetOverviewAsync(
        ReportFilterQuery query,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketReportExportRow>> GetTicketExportRowsAsync(
        ReportFilterQuery query,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
