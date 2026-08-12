namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
