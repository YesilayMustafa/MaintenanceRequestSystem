namespace MaintenanceRequestSystem.Application.Reports.Models;

public sealed record TicketReportExportRow(
    string TicketNumber,
    string Title,
    string Category,
    string Status,
    string Priority,
    DateTime CreatedAt,
    DateTime SlaDueAt,
    string SlaStatus,
    string CreatedBy,
    string Department,
    string? AssignedTechnician);

public sealed record ReportCsvFile(
    byte[] Content,
    string ContentType,
    string FileName);
