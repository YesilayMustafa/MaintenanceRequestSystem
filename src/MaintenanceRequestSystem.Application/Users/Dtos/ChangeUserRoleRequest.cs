using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Users.Dtos;

public sealed class ChangeUserRoleRequest
{
    public UserRole Role { get; init; }
}