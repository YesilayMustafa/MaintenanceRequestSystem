using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Models;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(string password);

    PasswordVerificationOutcome VerifyPassword(
        string? passwordHash,
        string providedPassword);
}
