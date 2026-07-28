using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;

    public AuthController(
        IAuthenticationService authenticationService)
    {
        _authenticationService = authenticationService;
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
    public ActionResult<CurrentUserDto> GetCurrentUser()
    {
        var userIdValue =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        var fullName =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Name);

        var email =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Email);

        var role =
            User.FindFirstValue("role");

        if (!Guid.TryParse(userIdValue, out var userId) ||
            string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(role))
        {
            return Unauthorized();
        }

        return Ok(
            new CurrentUserDto(
                userId,
                fullName,
                email,
                role));
    }
}