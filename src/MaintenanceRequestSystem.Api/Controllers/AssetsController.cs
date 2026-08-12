using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/assets")]
[Authorize]
public sealed class AssetsController : ControllerBase
{
    private readonly IAssetService _assetService;
    private readonly IAssetMaintenanceHistoryService _maintenanceHistoryService;

    public AssetsController(
        IAssetService assetService,
        IAssetMaintenanceHistoryService maintenanceHistoryService)
    {
        _assetService = assetService;
        _maintenanceHistoryService = maintenanceHistoryService;
    }

    [HttpGet]
    [ProducesResponseType(
        typeof(IReadOnlyList<AssetDto>),
        StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AssetDto>>> GetAll(
        CancellationToken cancellationToken)
    {
        var assets =
            await _assetService.GetAllAsync(
                cancellationToken);

        return Ok(assets);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(AssetDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var asset =
            await _assetService.GetByIdAsync(
                id,
                cancellationToken);

        return Ok(asset);
    }

    [HttpPost]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(
        typeof(AssetDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> Create(
        CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset =
            await _assetService.CreateAsync(
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = asset.Id },
            asset);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(
        typeof(AssetDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    [ProducesResponseType(
        StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDto>> Update(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var asset =
            await _assetService.UpdateAsync(
                id,
                request,
                cancellationToken);

        return Ok(asset);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(
        StatusCodes.Status204NoContent)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeStatus(
        Guid id,
        ChangeAssetStatusRequest request,
        CancellationToken cancellationToken)
    {
        await _assetService.ChangeStatusAsync(
            id,
            request,
            cancellationToken);

        return NoContent();
    }

    [HttpGet("{id:guid}/maintenance-history")]
    [ProducesResponseType(
        typeof(AssetMaintenanceHistoryDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AssetMaintenanceHistoryDto>>
        GetMaintenanceHistory(
            Guid id,
            [FromQuery] AssetMaintenanceHistoryQuery query,
            CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(out var userId, out var role))
        {
            return Unauthorized();
        }

        return Ok(await _maintenanceHistoryService.GetAsync(
            id,
            userId,
            role,
            query,
            cancellationToken));
    }

    private bool TryGetCurrentUser(out Guid userId, out UserRole role)
    {
        var validUserId = Guid.TryParse(
            User.FindFirstValue(JwtRegisteredClaimNames.Sub),
            out userId);
        var validRole = Enum.TryParse(
                User.FindFirstValue("role"),
                ignoreCase: true,
                out role) &&
            Enum.IsDefined(role);

        return validUserId && validRole;
    }
}
