using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MaintenanceRequestSystem.Application.Common.Models;

namespace MaintenanceRequestSystem.Api.Controllers;

/// <summary>
/// Ticket oluşturma, listeleme, görüntüleme ve atama HTTP endpoint'lerini sunar.
/// </summary>
[ApiController]
[Route("api/tickets")]
[Authorize(
    Roles =
        nameof(UserRole.Employee) + "," +
        nameof(UserRole.Technician) + "," +
        nameof(UserRole.Admin))]
public sealed class TicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    /// <summary>
    /// Controller'ı ticket use case sözleşmesiyle oluşturur.
    /// </summary>
    public TicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    /// <summary>
    /// JWT claim'lerindeki kullanıcı adına yeni ticket oluşturur.
    /// </summary>
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

    /// <summary>
    /// Route'taki atanmış ticket'ı request body'deki farklı ve aktif Technician kullanıcısına yeniden atar.
    /// İşlemi yapan Admin kimliği ve rolü JWT claim'lerinden alınır.
    /// Atama değişikliği ve history domain davranışında birlikte kaydedilir.
    /// </summary>
    [HttpPatch("{id:guid}/reassignment")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(
    typeof(TicketDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Reassign(
    Guid id,
    AssignTicketRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketService.ReassignAsync(
                id,
                userId,
                role,
                request,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// JWT claim'lerindeki kullanıcı ve rol kapsamına göre ticket listesini getirir.
    /// </summary>
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

    /// <summary>
    /// Route'taki açık ticket'ı request body'deki aktif Technician kullanıcısına ilk kez atar.
    /// İşlemi yapan Admin kimliği ve rolü JWT claim'lerinden alınır.
    /// Durum geçişi ve history domain davranışında birlikte oluşturulur.
    /// </summary>
    [HttpPatch("{id:guid}/assignment")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(
    typeof(TicketDto),
    StatusCodes.Status200OK)]
    [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
    StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Assign(
    Guid id,
    AssignTicketRequest request,
    CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketService.AssignAsync(
                id,
                userId,
                role,
                request,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// Route'taki ticket'ın detayını JWT claim'lerindeki kullanıcı ve role göre getirir.
    /// </summary>
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
            Enum.TryParse<UserRole>(
                roleValue,
                ignoreCase: true,
                out role) &&
            Enum.IsDefined(
                typeof(UserRole),
                role);

        return validUserId && validRole;
    }
}
