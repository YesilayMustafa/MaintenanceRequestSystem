namespace MaintenanceRequestSystem.Application.Reports.Dtos;

public sealed record ReportOverviewDto(
    ReportSummaryDto Summary,
    IReadOnlyList<ReportDistributionItemDto> ByStatus,
    IReadOnlyList<ReportDistributionItemDto> ByPriority,
    IReadOnlyList<ReportDistributionItemDto> ByCategory,
    IReadOnlyList<ReportTrendItemDto> DailyCreationTrend,
    IReadOnlyList<TechnicianPerformanceDto> TechnicianPerformance);

public sealed record ReportSummaryDto(
    int TotalTickets,
    int ActiveTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int CancelledTickets,
    int CriticalTickets,
    int SlaMetCount,
    int SlaBreachedCount,
    decimal SlaComplianceRate);

public sealed record ReportDistributionItemDto(
    string Key,
    string Label,
    int Count);

public sealed record ReportTrendItemDto(
    DateOnly Date,
    int Count);

public sealed record TechnicianPerformanceDto(
    Guid TechnicianId,
    string FullName,
    int AssignedCount,
    int ActiveCount,
    int ResolvedOrClosedCount,
    int SlaMetCount,
    int SlaBreachedCount,
    decimal SlaComplianceRate);
