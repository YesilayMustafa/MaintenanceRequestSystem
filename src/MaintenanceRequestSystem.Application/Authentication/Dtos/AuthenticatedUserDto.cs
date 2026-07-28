using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed record AuthenticatedUserDto(
    Guid Id,
    string FullName,
    string Email,
    string Role);