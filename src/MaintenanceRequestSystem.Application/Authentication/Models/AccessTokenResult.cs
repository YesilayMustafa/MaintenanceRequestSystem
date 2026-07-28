using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Authentication.Models;

public sealed record AccessTokenResult(
    string AccessToken,
    DateTime ExpiresAt);