using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Api.Controllers;

[ApiController]
[Route("api/tickets")]
[Authorize]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpPost]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status201Created)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Create(
        CreateTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out _))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketService.CreateAsync(
                userId,
                request,
                cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = ticket.Id },
            ticket);
    }

    [HttpGet]
    [ProducesResponseType(
    typeof(PagedResult<TicketDto>),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<TicketDto>>> GetAll(
    [FromQuery] TicketListQuery query,
    CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var result =
            await _ticketService.GetPagedAsync(
                userId,
                role,
                query,
                cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketService.GetByIdAsync(
                id,
                userId,
                role,
                cancellationToken);

        return Ok(ticket);
    }

    private bool TryGetCurrentUser(
        out Guid userId,
        out UserRole role)
    {
        var userIdValue =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Sub);

        var roleValue =
            User.FindFirstValue("role");

        var validUserId =
            Guid.TryParse(
                userIdValue,
                out userId);

        var validRole =
            Enum.TryParse(
                roleValue,
                ignoreCase: true,
                out role);

        return validUserId && validRole;
    }
}