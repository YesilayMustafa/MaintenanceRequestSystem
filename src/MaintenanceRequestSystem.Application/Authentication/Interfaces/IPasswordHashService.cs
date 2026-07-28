using System;
using System.Collections.Generic;
using System.Text;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IPasswordHashService
{
    string HashPassword(string password);

    bool VerifyPassword(
        string passwordHash,
        string providedPassword);
}