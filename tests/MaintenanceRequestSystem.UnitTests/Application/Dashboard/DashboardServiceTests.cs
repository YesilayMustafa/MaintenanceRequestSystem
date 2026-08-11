using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Dashboard.Dtos;
using MaintenanceRequestSystem.Application.Dashboard.Interfaces;
using MaintenanceRequestSystem.Application.Dashboard.Services;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Dashboard;

public sealed class DashboardServiceTests
{
    [Fact]
    public async Task GetAsync_WithOperationalUser_ReturnsScopedDashboard()
    {
        var user = CreateUser(UserRole.Employee);
        var expected = EmptyDashboard();
        var dashboardRepository =
            new FakeDashboardRepository(expected);

        var service = new DashboardService(
            dashboardRepository,
            new FakeUserRepository(user));

        var result = await service.GetAsync(
            user.Id,
            UserRole.Employee);

        Assert.Same(expected, result);
        Assert.Equal(user.Id, dashboardRepository.CurrentUserId);
        Assert.Equal(
            UserRole.Employee,
            dashboardRepository.CurrentUserRole);
    }

    [Fact]
    public async Task GetAsync_WithInactiveUser_ThrowsForbiddenException()
    {
        var user = CreateUser(UserRole.Employee);
        user.Deactivate();

        var service = new DashboardService(
            new FakeDashboardRepository(EmptyDashboard()),
            new FakeUserRepository(user));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetAsync(user.Id, UserRole.Employee));
    }

    [Fact]
    public async Task GetAsync_WithMismatchedRole_ThrowsForbiddenException()
    {
        var user = CreateUser(UserRole.Employee);

        var service = new DashboardService(
            new FakeDashboardRepository(EmptyDashboard()),
            new FakeUserRepository(user));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetAsync(user.Id, UserRole.Admin));
    }

    [Fact]
    public async Task GetAsync_WithUnsupportedRole_ThrowsForbiddenException()
    {
        var service = new DashboardService(
            new FakeDashboardRepository(EmptyDashboard()),
            new FakeUserRepository(null));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetAsync(Guid.NewGuid(), (UserRole)999));
    }

    private static User CreateUser(UserRole role)
    {
        return new User(
            "Dashboard Kullanıcısı",
            $"dashboard-{Guid.NewGuid():N}@example.com",
            "hashed-password",
            role,
            Guid.NewGuid());
    }

    private static DashboardDto EmptyDashboard()
    {
        return new DashboardDto(
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            [],
            null);
    }

    private sealed class FakeDashboardRepository
        : IDashboardRepository
    {
        private readonly DashboardDto _dashboard;

        public FakeDashboardRepository(DashboardDto dashboard)
        {
            _dashboard = dashboard;
        }

        public Guid CurrentUserId { get; private set; }

        public UserRole CurrentUserRole { get; private set; }

        public Task<DashboardDto> GetAsync(
            Guid currentUserId,
            UserRole currentUserRole,
            CancellationToken cancellationToken = default)
        {
            CurrentUserId = currentUserId;
            CurrentUserRole = currentUserRole;
            return Task.FromResult(_dashboard);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User? _user;

        public FakeUserRepository(User? user)
        {
            _user = user;
        }

        public Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(
                _user is null ? [] : [_user]);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_user);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedUserId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
