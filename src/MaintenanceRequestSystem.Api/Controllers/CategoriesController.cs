using MaintenanceRequestSystem.Api.Authentication;
using MaintenanceRequestSystem.Application.Categories.Dtos;
using MaintenanceRequestSystem.Application.Categories.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/categories")]
[Authorize]
public sealed class CategoriesController : ControllerBase
{
    private readonly ITicketCategoryService _categoryService;
    private readonly ICurrentUserAccessor _currentUserAccessor;

    public CategoriesController(
        ITicketCategoryService categoryService,
        ICurrentUserAccessor currentUserAccessor)
    {
        _categoryService = categoryService;
        _currentUserAccessor = currentUserAccessor;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TicketCategoryDto>>> GetAll(
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(out _, out var role))
        {
            return Unauthorized();
        }

        var categories = await _categoryService.GetAllAsync(
            includeInactive,
            role,
            cancellationToken);

        return Ok(categories);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketCategoryDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(out _, out var role))
        {
            return Unauthorized();
        }

        return Ok(await _categoryService.GetByIdAsync(
            id,
            role,
            cancellationToken));
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<TicketCategoryDto>> Create(
        CreateTicketCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var category = await _categoryService.CreateAsync(
            userId,
            role,
            request,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = category.Id },
            category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<ActionResult<TicketCategoryDto>> Update(
        Guid id,
        UpdateTicketCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        return Ok(await _categoryService.UpdateAsync(
            id,
            userId,
            role,
            request,
            cancellationToken));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeTicketCategoryStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!_currentUserAccessor.TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        await _categoryService.ChangeStatusAsync(
            id,
            userId,
            role,
            request,
            cancellationToken);

        return NoContent();
    }
}
