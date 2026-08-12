using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed record CurrentUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role,
    Guid DepartmentId,
    string DepartmentName,
    bool IsActive,
    string AccountStatus);
