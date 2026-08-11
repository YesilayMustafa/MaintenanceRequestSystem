using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Users.Services;

public sealed class UserService : IUserService
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IAuditLogService _auditLogService;

    public UserService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IPasswordHashService passwordHashService,
        IAuditLogService auditLogService)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _passwordHashService = passwordHashService;
        _auditLogService = auditLogService;
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var users =
            await _userRepository.GetAllAsync(
                cancellationToken);

        return users
    .Select(user => MapToDto(user))
    .ToList();
    }

    public async Task<UserDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);

        var user =
            await _userRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Kullanıcı bulunamadı.");
        }

        return MapToDto(user);
    }

    public async Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidatePassword(request.Password);

        var emailExists =
            await _userRepository.EmailExistsAsync(
                request.Email,
                cancellationToken: cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
                "Bu e-posta adresiyle kayıtlı bir kullanıcı zaten var.");
        }

        var department =
            await _departmentRepository.GetByIdAsync(
                request.DepartmentId,
                cancellationToken);

        if (department is null)
        {
            throw new KeyNotFoundException(
                "Seçilen departman bulunamadı.");
        }

        if (!department.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir departmana kullanıcı atanamaz.");
        }

        var passwordHash =
            _passwordHashService.HashPassword(
                request.Password);

        var user = new User(
            request.FullName,
            request.Email,
            passwordHash,
            request.Role,
            request.DepartmentId);

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            user,
            department.Name);
    }

    public async Task<UserDto> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        ArgumentNullException.ThrowIfNull(request);

        var user =
            await _userRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Kullanıcı bulunamadı.");
        }

        var emailExists =
            await _userRepository.EmailExistsAsync(
                request.Email,
                excludedUserId: id,
                cancellationToken: cancellationToken);

        if (emailExists)
        {
            throw new ConflictException(
                "Bu e-posta adresi başka bir kullanıcı tarafından kullanılıyor.");
        }

        var department =
            await _departmentRepository.GetByIdAsync(
                request.DepartmentId,
                cancellationToken);

        if (department is null)
        {
            throw new KeyNotFoundException(
                "Seçilen departman bulunamadı.");
        }

        if (!department.IsActive)
        {
            throw new RequestValidationException(
                "Pasif bir departmana kullanıcı atanamaz.");
        }

        user.UpdateDetails(
            request.FullName,
            request.Email,
            request.DepartmentId);

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(
            user,
            department.Name);
    }

    public Task ChangeStatusAsync(
        Guid id,
        Guid performedByUserId,
        ChangeUserStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        EnsureValidId(performedByUserId);
        ArgumentNullException.ThrowIfNull(request);

        return _userRepository.ExecuteInTransactionAsync(
            transactionCancellationToken =>
                ChangeStatusCoreAsync(
                    id,
                    performedByUserId,
                    request,
                    transactionCancellationToken),
            cancellationToken);
    }

    private async Task ChangeStatusCoreAsync(
        Guid id,
        Guid performedByUserId,
        ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {

        var user =
            await _userRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Kullanıcı bulunamadı.");
        }

        if (!request.IsActive && id == performedByUserId)
        {
            throw new ForbiddenException(
                "Admin kendi hesabını pasifleştiremez.");
        }

        if (!request.IsActive &&
            user.IsActive &&
            user.Role == UserRole.Admin)
        {
            await EnsureAnotherActiveAdminExistsAsync(
                user.Id,
                cancellationToken);
        }

        var oldIsActive = user.IsActive;

        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        if (oldIsActive != user.IsActive)
        {
            await _auditLogService.AddAsync(
                performedByUserId,
                user.IsActive
                    ? "UserActivated"
                    : "UserDeactivated",
                nameof(User),
                user.Id.ToString(),
                new
                {
                    IsActive = oldIsActive
                },
                new
                {
                    user.IsActive
                },
                cancellationToken);
        }

        await _userRepository.SaveChangesAsync(
            cancellationToken);
    }

    public Task ChangeRoleAsync(
        Guid id,
        Guid performedByUserId,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(id);
        EnsureValidId(performedByUserId);
        ArgumentNullException.ThrowIfNull(request);

        return _userRepository.ExecuteInTransactionAsync(
            transactionCancellationToken =>
                ChangeRoleCoreAsync(
                    id,
                    performedByUserId,
                    request,
                    transactionCancellationToken),
            cancellationToken);
    }

    private async Task ChangeRoleCoreAsync(
        Guid id,
        Guid performedByUserId,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {

        var user =
            await _userRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Kullanıcı bulunamadı.");
        }

        if (user.IsActive &&
            user.Role == UserRole.Admin &&
            request.Role != UserRole.Admin)
        {
            await EnsureAnotherActiveAdminExistsAsync(
                user.Id,
                cancellationToken);
        }

        var oldRole = user.Role;

        user.ChangeRole(request.Role);

        if (oldRole != user.Role)
        {
            await _auditLogService.AddAsync(
                performedByUserId,
                "UserRoleChanged",
                nameof(User),
                user.Id.ToString(),
                new
                {
                    Role = oldRole
                },
                new
                {
                    user.Role
                },
                cancellationToken);
        }

        await _userRepository.SaveChangesAsync(
            cancellationToken);
    }

    private static void ValidatePassword(
        string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new RequestValidationException(
                "Parola boş olamaz.");
        }

        if (password.Length < MinPasswordLength)
        {
            throw new RequestValidationException(
                $"Parola en az {MinPasswordLength} karakter olmalıdır.");
        }

        if (password.Length > MaxPasswordLength)
        {
            throw new RequestValidationException(
                $"Parola en fazla {MaxPasswordLength} karakter olabilir.");
        }
    }

    private static void EnsureValidId(Guid id)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                "Geçerli bir kullanıcı kimliği gereklidir.");
        }
    }

    private async Task EnsureAnotherActiveAdminExistsAsync(
        Guid excludedUserId,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(
            cancellationToken);

        var anotherActiveAdminExists = users.Any(user =>
            user.Id != excludedUserId &&
            user.IsActive &&
            user.Role == UserRole.Admin);

        if (!anotherActiveAdminExists)
        {
            throw new ConflictException(
                "Sistemde en az bir aktif Admin kalmalıdır.");
        }
    }

    private static UserDto MapToDto(
        User user,
        string? departmentName = null)
    {
        return new UserDto(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.DepartmentId,
            departmentName ??
            user.Department?.Name ??
            string.Empty,
            user.IsActive,
            user.CreatedAt,
            user.UpdatedAt,
            GetAccountStatus(user));
    }

    private static string GetAccountStatus(User user)
    {
        if (!user.IsActive)
        {
            return "Inactive";
        }

        return user.InvitationAcceptedAt.HasValue
            ? "Active"
            : "PendingInvitation";
    }
}
