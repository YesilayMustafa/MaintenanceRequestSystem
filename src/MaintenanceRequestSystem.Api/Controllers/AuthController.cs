using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Api.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public AuthController(
        IAuthenticationService authenticationService,
        ICurrentUserAccessor currentUserAccessor)
    {
        _authenticationService = authenticationService;
        _currentUserAccessor = currentUserAccessor;
    }

    [AllowAnonymous]
    [HttpPost("login")]
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
}
