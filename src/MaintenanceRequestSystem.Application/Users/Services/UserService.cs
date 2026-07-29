using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Users.Services;

public sealed class UserService : IUserService
{
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;

    private readonly IUserRepository _userRepository;
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IPasswordHashService _passwordHashService;

    public UserService(
        IUserRepository userRepository,
        IDepartmentRepository departmentRepository,
        IPasswordHashService passwordHashService)
    {
        _userRepository = userRepository;
        _departmentRepository = departmentRepository;
        _passwordHashService = passwordHashService;
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

    public async Task ChangeStatusAsync(
        Guid id,
        ChangeUserStatusRequest request,
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

        if (request.IsActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await _userRepository.SaveChangesAsync(
            cancellationToken);
    }

    public async Task ChangeRoleAsync(
        Guid id,
        ChangeUserRoleRequest request,
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

        user.ChangeRole(request.Role);

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
            user.UpdatedAt);
    }
}