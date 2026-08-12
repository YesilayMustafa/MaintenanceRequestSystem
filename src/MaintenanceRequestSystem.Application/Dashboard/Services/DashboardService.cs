using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Application.Dashboard.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Dashboard.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IUserRepository _userRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IUserRepository userRepository)
    {
        _dashboardRepository = dashboardRepository;
        _userRepository = userRepository;
    }

    public async Task<DashboardDto> GetAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        if (currentUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Geçerli bir kullanıcı kimliği gereklidir.",
                nameof(currentUserId));
        }

        if (!Enum.IsDefined(currentUserRole))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }

        var currentUser =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException(
                "Kullanıcı bulunamadı.");
        }

        if (!currentUser.IsOperational)
        {
            throw new ForbiddenException(
                "Aktif olmayan kullanıcılar dashboard görüntüleyemez.");
        }

        if (currentUser.Role != currentUserRole)
        {
            throw new ForbiddenException(
                "Kullanıcı rolü doğrulanamadı.");
        }

        return await _dashboardRepository.GetAsync(
            currentUserId,
            currentUserRole,
            cancellationToken);
    }
}
