using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;

namespace MaintenanceRequestSystem.Application.Authentication.Interfaces;

public interface IAccountLifecycleService
{
    Task<UserDto> InviteUserAsync(
        Guid performedByUserId,
        InviteUserRequest request,
        CancellationToken cancellationToken = default);

    Task ResendInvitationAsync(
        Guid userId,
        Guid performedByUserId,
        CancellationToken cancellationToken = default);

    Task AcceptInvitationAsync(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken = default);

    Task<ForgotPasswordResponse> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default);
}
