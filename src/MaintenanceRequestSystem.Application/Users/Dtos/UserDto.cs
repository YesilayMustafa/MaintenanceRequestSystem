using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid DepartmentId,
    string DepartmentName,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);