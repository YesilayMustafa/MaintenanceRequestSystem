using MaintenanceRequestSystem.Application.Common.Exceptions;

namespace MaintenanceRequestSystem.Application.Authentication.Services;

public static class PasswordPolicy
{
    public const int MinLength = 8;
    public const int MaxLength = 128;

    public static void EnsureValid(string? password)
    {
        if (string.IsNullOrEmpty(password))
        {
            throw new RequestValidationException(
                "Parola boş olamaz.");
        }

        if (password.Length < MinLength)
        {
            throw new RequestValidationException(
                $"Parola en az {MinLength} karakter olmalıdır.");
        }

        if (password.Length > MaxLength)
        {
            throw new RequestValidationException(
                $"Parola en fazla {MaxLength} karakter olabilir.");
        }
    }
}
