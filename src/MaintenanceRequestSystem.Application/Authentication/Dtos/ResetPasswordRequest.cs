namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed record ResetPasswordRequest(
    string Token,
    string NewPassword);
