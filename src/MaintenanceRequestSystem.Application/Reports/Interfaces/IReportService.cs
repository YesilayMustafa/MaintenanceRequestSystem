using MaintenanceRequestSystem.Application.Reports.Dtos;
using MaintenanceRequestSystem.Application.Reports.Models;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Reports.Interfaces;

public interface IReportService
{
    Task<ReportOverviewDto> GetOverviewAsync(
        UserRole currentUserRole,
        ReportFilterQuery query,
        CancellationToken cancellationToken = default);

    Task<ReportCsvFile> ExportTicketsAsync(
        UserRole currentUserRole,
        ReportFilterQuery query,
        CancellationToken cancellationToken = default);
}
