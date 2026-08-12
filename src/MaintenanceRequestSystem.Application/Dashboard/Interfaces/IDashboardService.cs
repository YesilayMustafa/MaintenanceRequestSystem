using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Dashboard.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
}
