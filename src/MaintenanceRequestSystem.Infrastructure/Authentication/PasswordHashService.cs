using System;
using System.Collections.Generic;
using System.Text;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Application.Authentication.Models;
using Microsoft.AspNetCore.Identity;
using IdentityPasswordVerificationResult =
    Microsoft.AspNetCore.Identity.PasswordVerificationResult;

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

    public PasswordVerificationOutcome VerifyPassword(
        string? passwordHash,
        string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) ||
            string.IsNullOrWhiteSpace(providedPassword))
        {
            return PasswordVerificationOutcome.Failed;
        }

        var result =
            _passwordHasher.VerifyHashedPassword(
                Context,
                passwordHash,
                providedPassword);

        return result switch
        {
            IdentityPasswordVerificationResult.Success =>
                PasswordVerificationOutcome.Success,

            IdentityPasswordVerificationResult.SuccessRehashNeeded =>
                PasswordVerificationOutcome.SuccessRehashNeeded,

            _ => PasswordVerificationOutcome.Failed
        };
    }

    private sealed class PasswordHashContext
    {
    }
}
