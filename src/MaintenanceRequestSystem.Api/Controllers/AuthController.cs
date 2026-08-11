using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Api.Authentication;
using MaintenanceRequestSystem.Api.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IAccountLifecycleService _accountLifecycleService;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AuthController(
        IAuthenticationService authenticationService,
        IAccountLifecycleService accountLifecycleService,
        ICurrentUserAccessor currentUserAccessor)
    {
        _authenticationService = authenticationService;
        _accountLifecycleService = accountLifecycleService;
        _currentUserAccessor = currentUserAccessor;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting(AccountRateLimitPolicyNames.Login)]
    [ProducesResponseType(
        typeof(LoginResponse),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response =
            await _authenticationService.LoginAsync(
                request,
                cancellationToken);

        return Ok(response);


    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(
    typeof(CurrentUserDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CurrentUserDto>> GetCurrentUser(
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(
                out var userId,
                out _))
        {
            return Unauthorized();
        }

        var currentUser =
            await _authenticationService.GetCurrentUserAsync(
                userId,
                cancellationToken);

        return Ok(currentUser);
    }

    [AllowAnonymous]
    [HttpPost("invitations/accept")]
    [EnableRateLimiting(
        AccountRateLimitPolicyNames.AcceptInvitation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AcceptInvitation(
        AcceptInvitationRequest request,
        CancellationToken cancellationToken)
    {
        await _accountLifecycleService.AcceptInvitationAsync(
            request,
            cancellationToken);

        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting(
        AccountRateLimitPolicyNames.ForgotPassword)]
    [ProducesResponseType(
        typeof(ForgotPasswordResponse),
        StatusCodes.Status202Accepted)]
    public async Task<ActionResult<ForgotPasswordResponse>>
        ForgotPassword(
            ForgotPasswordRequest request,
            CancellationToken cancellationToken)
    {
        var response =
            await _accountLifecycleService.ForgotPasswordAsync(
                request,
                cancellationToken);

        return Accepted(response);
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting(
        AccountRateLimitPolicyNames.ResetPassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ResetPassword(
        ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        await _accountLifecycleService.ResetPasswordAsync(
            request,
            cancellationToken);

        return NoContent();
    }

    [Authorize]
    [HttpPost("change-password")]
    [EnableRateLimiting(
        AccountRateLimitPolicyNames.ChangePassword)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(
                out var userId,
                out _))
        {
            return Unauthorized();
        }

        await _accountLifecycleService.ChangePasswordAsync(
            userId,
            request,
            cancellationToken);

        return NoContent();
    }
}
