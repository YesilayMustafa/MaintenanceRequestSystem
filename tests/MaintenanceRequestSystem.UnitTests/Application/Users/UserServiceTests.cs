using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Application.Users.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Users;

public sealed class UserServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_HashesPasswordAndSavesUser()
    {
        // Arrange
        var department = CreateDepartment();

        var userRepository = new FakeUserRepository();

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = department
            };

        var passwordHashService =
            new FakePasswordHashService
            {
                HashToReturn = "generated-password-hash"
            };

        var service = CreateService(
            userRepository,
            departmentRepository,
            passwordHashService);

        var request =
            new CreateUserRequest
            {
                FullName = "  Ahmet Yılmaz  ",
                Email = "  AHMET@EXAMPLE.COM  ",
                Password = "UserTest123!",
                Role = UserRole.Employee,
                DepartmentId = department.Id
            };

        // Act
        var result =
            await service.CreateAsync(request);

        // Assert
        Assert.True(userRepository.AddCalled);
        Assert.Equal(1, userRepository.SaveChangesCallCount);

        Assert.Equal(
            request.Password,
            passwordHashService.LastPlainPassword);

        var createdUser =
            Assert.Single(userRepository.Users);

        Assert.Equal(
            "generated-password-hash",
            createdUser.PasswordHash);

        Assert.Equal(
            "Ahmet Yılmaz",
            createdUser.FullName);

        Assert.Equal(
            "ahmet@example.com",
            createdUser.Email);

        Assert.Equal(
            UserRole.Employee,
            createdUser.Role);

        Assert.Equal(
            department.Id,
            createdUser.DepartmentId);

        Assert.Equal(
            createdUser.Id,
            result.Id);

        Assert.Equal(
            "Bilgi İşlem",
            result.DepartmentName);

        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateEmail_ThrowsConflictException()
    {
        // Arrange
        var userRepository =
            new FakeUserRepository
            {
                EmailExistsResult = true
            };

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = CreateDepartment()
            };

        var passwordHashService =
            new FakePasswordHashService();

        var service = CreateService(
            userRepository,
            departmentRepository,
            passwordHashService);

        var request =
            CreateUserRequestFor(
                departmentRepository.DepartmentById!.Id);

        // Act
        var exception =
            await Assert.ThrowsAsync<ConflictException>(
                () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "e-posta",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(userRepository.AddCalled);
        Assert.Equal(0, userRepository.SaveChangesCallCount);
        Assert.Null(passwordHashService.LastPlainPassword);
    }

    [Fact]
    public async Task CreateAsync_WithMissingDepartment_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userRepository =
            new FakeUserRepository();

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = null
            };

        var passwordHashService =
            new FakePasswordHashService();

        var service = CreateService(
            userRepository,
            departmentRepository,
            passwordHashService);

        var request =
            CreateUserRequestFor(Guid.NewGuid());

        // Act
        var exception =
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "departman",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(userRepository.AddCalled);
        Assert.Equal(0, userRepository.SaveChangesCallCount);
        Assert.Null(passwordHashService.LastPlainPassword);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveDepartment_ThrowsValidationException()
    {
        // Arrange
        var department =
            CreateDepartment(isActive: false);

        var userRepository =
            new FakeUserRepository();

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = department
            };

        var passwordHashService =
            new FakePasswordHashService();

        var service = CreateService(
            userRepository,
            departmentRepository,
            passwordHashService);

        var request =
            CreateUserRequestFor(department.Id);

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "pasif",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(userRepository.AddCalled);
        Assert.Equal(0, userRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithShortPassword_ThrowsValidationException()
    {
        // Arrange
        var department = CreateDepartment();

        var userRepository =
            new FakeUserRepository();

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = department
            };

        var passwordHashService =
            new FakePasswordHashService();

        var service = CreateService(
            userRepository,
            departmentRepository,
            passwordHashService);

        var request =
            new CreateUserRequest
            {
                FullName = "Ahmet Yılmaz",
                Email = "ahmet@example.com",
                Password = "123",
                Role = UserRole.Employee,
                DepartmentId = department.Id
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.CreateAsync(request));

        // Assert
        Assert.Contains(
            "en az",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(userRepository.AddCalled);
        Assert.Equal(0, userRepository.SaveChangesCallCount);
        Assert.Null(passwordHashService.LastPlainPassword);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAndSavesUser()
    {
        // Arrange
        var originalDepartmentId = Guid.NewGuid();

        var user =
            new User(
                "Eski Kullanıcı",
                "eski@example.com",
                "existing-password-hash",
                UserRole.Employee,
                originalDepartmentId);

        var newDepartment =
            CreateDepartment();

        var userRepository =
            new FakeUserRepository
            {
                UserById = user,
                EmailExistsResult = false
            };

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = newDepartment
            };

        var service = CreateService(
            userRepository,
            departmentRepository,
            new FakePasswordHashService());

        var request =
            new UpdateUserRequest
            {
                FullName = "Güncel Kullanıcı",
                Email = "guncel@example.com",
                DepartmentId = newDepartment.Id
            };

        // Act
        var result =
            await service.UpdateAsync(
                user.Id,
                request);

        // Assert
        Assert.Equal(
            "Güncel Kullanıcı",
            user.FullName);

        Assert.Equal(
            "guncel@example.com",
            user.Email);

        Assert.Equal(
            newDepartment.Id,
            user.DepartmentId);

        Assert.NotNull(user.UpdatedAt);

        Assert.Equal(1, userRepository.SaveChangesCallCount);

        Assert.Equal(
            "Bilgi İşlem",
            result.DepartmentName);
    }

    [Fact]
    public async Task ChangeRoleAsync_WithValidRole_ChangesRoleAndSaves()
    {
        // Arrange
        var user =
            CreateUser();

        var userRepository =
            new FakeUserRepository
            {
                UserById = user
            };

        var service = CreateService(
            userRepository,
            new FakeDepartmentRepository(),
            new FakePasswordHashService());

        var request =
            new ChangeUserRoleRequest
            {
                Role = UserRole.Technician
            };

        // Act
        await service.ChangeRoleAsync(
            user.Id,
            request);

        // Assert
        Assert.Equal(
            UserRole.Technician,
            user.Role);

        Assert.NotNull(user.UpdatedAt);
        Assert.Equal(1, userRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithInactiveRequest_DeactivatesAndSaves()
    {
        // Arrange
        var user =
            CreateUser();

        var userRepository =
            new FakeUserRepository
            {
                UserById = user
            };

        var service = CreateService(
            userRepository,
            new FakeDepartmentRepository(),
            new FakePasswordHashService());

        var request =
            new ChangeUserStatusRequest
            {
                IsActive = false
            };

        // Act
        await service.ChangeStatusAsync(
            user.Id,
            request);

        // Assert
        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
        Assert.Equal(1, userRepository.SaveChangesCallCount);
    }

    private static UserService CreateService(
        FakeUserRepository userRepository,
        FakeDepartmentRepository departmentRepository,
        FakePasswordHashService passwordHashService)
    {
        return new UserService(
            userRepository,
            departmentRepository,
            passwordHashService);
    }

    private static CreateUserRequest CreateUserRequestFor(
        Guid departmentId)
    {
        return new CreateUserRequest
        {
            FullName = "Ahmet Yılmaz",
            Email = "ahmet@example.com",
            Password = "UserTest123!",
            Role = UserRole.Employee,
            DepartmentId = departmentId
        };
    }

    private static User CreateUser()
    {
        return new User(
            "Ahmet Yılmaz",
            "ahmet@example.com",
            "existing-password-hash",
            UserRole.Employee,
            Guid.NewGuid());
    }

    private static Department CreateDepartment(
        bool isActive = true)
    {
        var department =
            new Department(
                "Bilgi İşlem",
                "Teknik destek ve sistem yönetimi");

        if (!isActive)
        {
            department.Deactivate();
        }

        return department;
    }

    private sealed class FakeUserRepository
        : IUserRepository
    {
        public List<User> Users { get; } = [];

        public User? UserById { get; init; }

        public bool EmailExistsResult { get; init; }

        public bool AddCalled { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>(
                Users);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var user =
                UserById ??
                Users.FirstOrDefault(
                    existingUser =>
                        existingUser.Id == id);

            return Task.FromResult(user);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            var normalizedEmail =
                email.Trim().ToLowerInvariant();

            var user =
                Users.FirstOrDefault(
                    existingUser =>
                        existingUser.Email ==
                        normalizedEmail);

            return Task.FromResult(user);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedUserId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                EmailExistsResult);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            AddCalled = true;
            Users.Add(user);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeDepartmentRepository
        : IDepartmentRepository
    {
        public Department? DepartmentById { get; init; }

        public Task<IReadOnlyList<Department>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Department> departments =
                DepartmentById is null
                    ? []
                    : [DepartmentById];

            return Task.FromResult(departments);
        }

        public Task<Department?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                DepartmentById);
        }

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludedDepartmentId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class FakePasswordHashService
        : IPasswordHashService
    {
        public string HashToReturn { get; init; } =
            "fake-password-hash";

        public string? LastPlainPassword { get; private set; }

        public string HashPassword(string password)
        {
            LastPlainPassword = password;

            return HashToReturn;
        }

        public bool VerifyPassword(
            string passwordHash,
            string providedPassword)
        {
            return passwordHash == providedPassword;
        }
    }
}