using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IJwtTokenService
{
    AccessTokenResult CreateToken(User user);
}