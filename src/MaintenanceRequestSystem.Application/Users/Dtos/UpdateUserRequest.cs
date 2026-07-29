using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Users.Dtos;

public sealed class UpdateUserRequest
{
    public string FullName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public Guid DepartmentId { get; init; }
}