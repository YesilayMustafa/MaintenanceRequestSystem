using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace MaintenanceRequestSystem.Infrastructure.Authentication;

public sealed class PasswordHashService : IPasswordHashService
{
    private static readonly PasswordHashContext Context = new();

    private readonly PasswordHasher<PasswordHashContext> _passwordHasher =
        new();

    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException(
                "Parola boş olamaz.",
                nameof(password));
        }

        return _passwordHasher.HashPassword(
            Context,
            password);
    }

    public bool VerifyPassword(
        string passwordHash,
        string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        var result =
            _passwordHasher.VerifyHashedPassword(
                Context,
                passwordHash,
                providedPassword);

        return result is
            PasswordVerificationResult.Success or
            PasswordVerificationResult.SuccessRehashNeeded;
    }

    private sealed class PasswordHashContext
    {
    }
}