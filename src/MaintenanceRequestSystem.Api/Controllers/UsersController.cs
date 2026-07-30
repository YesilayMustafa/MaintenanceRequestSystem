using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Roles = nameof(UserRole.Admin))]
public sealed class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<UserDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var users =
            await _userService.GetAllAsync(
                cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user =
            await _userService.GetByIdAsync(
                id,
                cancellationToken);

        return Ok(user);
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user =
            await _userService.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = user.Id },
            user);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(
        typeof(UserDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Update(
        Guid id,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user =
            await _userService.UpdateAsync(
                id,
                request,
                cancellationToken);

        return Ok(user);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ChangeStatusAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        ChangeUserRoleRequest request,
        CancellationToken cancellationToken)
    {
        await _userService.ChangeRoleAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }
}