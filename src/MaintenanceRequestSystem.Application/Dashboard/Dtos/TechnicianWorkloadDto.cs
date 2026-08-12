namespace MaintenanceRequestSystem.Application.Dashboard.Dtos;

public sealed record TechnicianWorkloadDto(
    Guid TechnicianId,
    string FullName,
    int ActiveTicketCount);
