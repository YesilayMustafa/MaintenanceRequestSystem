using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed class LoginRequest
{
    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}