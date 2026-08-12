namespace MaintenanceRequestSystem.Application.Authentication.Dtos;

public sealed record AcceptInvitationRequest(
    string Token,
    string NewPassword);
