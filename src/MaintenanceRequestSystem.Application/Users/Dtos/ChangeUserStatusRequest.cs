using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Users.Dtos;

public sealed class ChangeUserStatusRequest
{
    public bool IsActive { get; init; }
}