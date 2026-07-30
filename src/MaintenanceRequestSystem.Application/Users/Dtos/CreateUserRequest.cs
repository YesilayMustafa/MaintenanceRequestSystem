using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Users.Dtos;

public sealed class CreateUserRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public UserRole Role { get; init; }

    public Guid DepartmentId { get; init; }
}