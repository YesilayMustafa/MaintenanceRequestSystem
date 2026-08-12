using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Application.Authentication.Services;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Authentication;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task LoginAsync_WhenPasswordNeedsRehash_UpdatesHashBeforeCreatingToken()
    {
        // Arrange
        var user = CreateUser();
        var userRepository = new FakeUserRepository(user);

        var passwordHashService = new FakePasswordHashService
        {
            VerificationOutcome =
                PasswordVerificationOutcome.SuccessRehashNeeded,
            HashToReturn = "updated-password-hash"
        };

        var jwtTokenService = new FakeJwtTokenService();

        var service = new AuthenticationService(
            userRepository,
            passwordHashService,
            jwtTokenService);

        // Act
        await service.LoginAsync(
            new LoginRequest
            {
                Email = user.Email,
                Password = "PlainPassword123!"
            });

        // Assert
        Assert.Equal("updated-password-hash", user.PasswordHash);
        Assert.Equal(2, user.SecurityVersion);
        Assert.Equal(1, userRepository.SaveChangesCallCount);
        Assert.Same(user, jwtTokenService.LastUser);
        Assert.Equal(2, jwtTokenService.LastUser!.SecurityVersion);
    }

    [Fact]
    public async Task GetCurrentUserAsync_WithActiveUser_ReturnsDatabaseValues()
    {
        // Arrange
        var user = CreateUser();

        var service = new AuthenticationService(
            new FakeUserRepository(user),
            new FakePasswordHashService(),
            new FakeJwtTokenService());

        // Act
        var result = await service.GetCurrentUserAsync(user.Id);

        // Assert
        Assert.Equal(user.Id, result.Id);
        Assert.Equal(user.FullName, result.FullName);
        Assert.Equal(user.Email, result.Email);
        Assert.Equal(user.Role.ToString(), result.Role);
        Assert.Equal(user.DepartmentId, result.DepartmentId);
        Assert.True(result.IsActive);
        Assert.Equal("Active", result.AccountStatus);
    }

    private static User CreateUser()
    {
        return new User(
            "Test Kullanıcısı",
            "test.user@example.com",
            "existing-password-hash",
            UserRole.Employee,
            Guid.NewGuid());
    }

    private sealed class FakeUserRepository
        : IUserRepository
    {
        private readonly User _user;

        public FakeUserRepository(User user)
        {
            _user = user;
        }

        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>([_user]);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(
                id == _user.Id ? _user : null);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(
                string.Equals(
                    email.Trim(),
                    _user.Email,
                    StringComparison.OrdinalIgnoreCase)
                    ? _user
                    : null);
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
            SaveChangesCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHashService
        : IPasswordHashService
    {
        public PasswordVerificationOutcome VerificationOutcome { get; init; }
            = PasswordVerificationOutcome.Success;

        public string HashToReturn { get; init; } = "new-password-hash";

        public string HashPassword(string password)
        {
            return HashToReturn;
        }

        public PasswordVerificationOutcome VerifyPassword(
            string? passwordHash,
            string providedPassword)
        {
            return VerificationOutcome;
        }
    }

    private sealed class FakeJwtTokenService
        : IJwtTokenService
    {
        public User? LastUser { get; private set; }

        public AccessTokenResult CreateToken(User user)
        {
            LastUser = user;

            return new AccessTokenResult(
                "test-access-token",
                DateTime.UtcNow.AddHours(1));
        }
    }
}
