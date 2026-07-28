using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthenticatedUserDto User);