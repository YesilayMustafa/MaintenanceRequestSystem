namespace MaintenanceRequestSystem.Application.Dashboard.Dtos;

public sealed record AdminDashboardDto(
    int UnassignedOpenCount,
    IReadOnlyList<TechnicianWorkloadDto> TechnicianWorkload);
