using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
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

    public AssetsController(IAssetService assetService)
    {
        _assetService = assetService;
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
}