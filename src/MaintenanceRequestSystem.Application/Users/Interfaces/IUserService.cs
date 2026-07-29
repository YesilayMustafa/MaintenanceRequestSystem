using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Users.Dtos;

namespace MaintenanceRequestSystem.Application.Users.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<UserDto> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(
        CreateUserRequest request,
        CancellationToken cancellationToken = default);

    Task<UserDto> UpdateAsync(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken = default);

    Task ChangeStatusAsync(
        Guid id,
        ChangeUserStatusRequest request,
        CancellationToken cancellationToken = default);

    Task ChangeRoleAsync(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken = default);
}