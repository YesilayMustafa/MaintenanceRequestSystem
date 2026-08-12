namespace MaintenanceRequestSystem.Application.Authentication.Models;

public sealed record AccountLifecycleSettings(
    TimeSpan InvitationLifetime,
    TimeSpan PasswordResetLifetime,
    string FrontendBaseUrl);
