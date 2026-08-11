namespace MaintenanceRequestSystem.Application.Authentication.Models;

public readonly record struct PasswordVerificationOutcome(
    bool Succeeded,
    bool NeedsRehash)
{
    public static PasswordVerificationOutcome Failed =>
        new(false, false);

    public static PasswordVerificationOutcome Success =>
        new(true, false);

    public static PasswordVerificationOutcome SuccessRehashNeeded =>
        new(true, true);
}
