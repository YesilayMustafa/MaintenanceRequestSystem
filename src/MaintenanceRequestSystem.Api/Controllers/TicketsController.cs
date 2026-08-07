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
    private readonly ITicketQueryService _ticketQueryService;
    private readonly ITicketCreationService _ticketCreationService;
    private readonly ITicketAssignmentService _ticketAssignmentService;
    private readonly ITicketTechnicianLifecycleService _ticketTechnicianLifecycleService;
    private readonly ITicketCompletionService _ticketCompletionService;
    private readonly ITicketAdministrationService _ticketAdministrationService;

    /// <summary>
    /// Controller'ı ticket use case sözleşmesiyle oluşturur.
    /// </summary>
    public TicketsController(
        ITicketQueryService ticketQueryService,
        ITicketCreationService ticketCreationService,
        ITicketAssignmentService ticketAssignmentService,
        ITicketTechnicianLifecycleService ticketTechnicianLifecycleService,
        ITicketCompletionService ticketCompletionService,
        ITicketAdministrationService ticketAdministrationService)
    {
        _ticketQueryService = ticketQueryService;
        _ticketCreationService = ticketCreationService;
        _ticketAssignmentService = ticketAssignmentService;
        _ticketTechnicianLifecycleService = ticketTechnicianLifecycleService;
        _ticketCompletionService = ticketCompletionService;
        _ticketAdministrationService = ticketAdministrationService;
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
            await _ticketCreationService.CreateAsync(
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
            await _ticketAssignmentService.ReassignAsync(
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
            await _ticketQueryService.GetPagedAsync(
                userId,
                role,
                query,
                cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Talebe atanmış teknik personelin çalışmayı başlatmasını sağlar.
    /// Kullanıcı kimliği ve rolü JWT claim'lerinden alınır.
    /// </summary>
    [HttpPatch("{id:guid}/start-progress")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> StartProgress(
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
            await _ticketTechnicianLifecycleService.StartProgressAsync(
                id,
                userId,
                role,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// İşlemdeki talebi, request body'deki gerekçeyle beklemeye alır.
    /// İşlemi yalnızca talebe atanmış aktif teknik personel yapabilir.
    /// </summary>
    [HttpPatch("{id:guid}/put-on-hold")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> PutOnHold(
        Guid id,
        PutTicketOnHoldRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketTechnicianLifecycleService.PutOnHoldAsync(
                id,
                userId,
                role,
                request,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// İşlemdeki talebi, request body'deki çözüm açıklamasıyla
    /// Resolved durumuna geçirir.
    /// İşlemi yalnızca talebe atanmış aktif teknik personel yapabilir.
    /// </summary>
    [HttpPatch("{id:guid}/resolve")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Resolve(
        Guid id,
        ResolveTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketTechnicianLifecycleService.ResolveAsync(
                id,
                userId,
                role,
                request,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// Çözümlenmiş talebi kapatır.
    /// İşlemi yalnızca talep sahibi Employee veya Admin yapabilir.
    /// </summary>
    [HttpPatch("{id:guid}/close")]
    [Authorize(
        Roles =
            nameof(UserRole.Employee) + "," +
            nameof(UserRole.Admin))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Close(
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
            await _ticketCompletionService.CloseAsync(
                id,
                userId,
                role,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// Kapatılmış talebi, belirtilen gerekçeyle yeniden işleme alır.
    /// İşlemi yalnızca talep sahibi Employee veya Admin yapabilir.
    /// </summary>
    [HttpPatch("{id:guid}/reopen")]
    [Authorize(
        Roles =
            nameof(UserRole.Employee) + "," +
            nameof(UserRole.Admin))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Reopen(
        Guid id,
        ReopenTicketRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketCompletionService.ReopenAsync(
                id,
                userId,
                role,
                request,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// İptale uygun talebi Cancelled durumuna geçirir.
    /// Talep sahibi yalnızca Open talebini, Admin ise Open,
    /// Assigned veya Waiting talebi iptal edebilir.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    [Authorize(
        Roles =
            nameof(UserRole.Employee) + "," +
            nameof(UserRole.Admin))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Cancel(
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
            await _ticketCompletionService.CancelAsync(
                id,
                userId,
                role,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// Aktif durumdaki talebin önceliğini değiştirir.
    /// İşlemi yalnızca aktif Admin yapabilir.
    /// </summary>
    [HttpPatch("{id:guid}/priority")]
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
    public async Task<ActionResult<TicketDto>> ChangePriority(
        Guid id,
        ChangeTicketPriorityRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var ticket =
            await _ticketAdministrationService.ChangePriorityAsync(
                id,
                userId,
                role,
                request,
                cancellationToken);

        return Ok(ticket);
    }

    /// <summary>
    /// Kullanıcının erişim yetkisi bulunan talebin
    /// durum değişikliği geçmişini getirir.
    /// </summary>
    [HttpGet("{id:guid}/history")]
    [Authorize(
        Roles =
            nameof(UserRole.Employee) + "," +
            nameof(UserRole.Technician) + "," +
            nameof(UserRole.Admin))]
    [ProducesResponseType(
        typeof(IReadOnlyList<TicketHistoryDto>),
        StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TicketHistoryDto>>> GetHistory(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        var histories =
            await _ticketQueryService.GetHistoryAsync(
                id,
                userId,
                role,
                cancellationToken);

        return Ok(histories);
    }

    /// <summary>
    /// Kapatılmış veya iptal edilmiş talebi soft delete ile pasifleştirir.
    /// İşlemi yalnızca aktif Admin yapabilir.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SoftDelete(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetCurrentUser(
                out var userId,
                out var role))
        {
            return Unauthorized();
        }

        await _ticketAdministrationService.SoftDeleteAsync(
            id,
            userId,
            role,
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Beklemedeki talebi yeniden işleme alır.
    /// İşlemi yalnızca talebe atanmış aktif teknik personel yapabilir.
    /// </summary>
    [HttpPatch("{id:guid}/resume")]
    [Authorize(Roles = nameof(UserRole.Technician))]
    [ProducesResponseType(
        typeof(TicketDto),
        StatusCodes.Status200OK)]
    [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
    [ProducesResponseType(
        StatusCodes.Status403Forbidden)]
    [ProducesResponseType(
        StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TicketDto>> Resume(
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
            await _ticketTechnicianLifecycleService.ResumeAsync(
                id,
                userId,
                role,
                cancellationToken);

        return Ok(ticket);
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
            await _ticketAssignmentService.AssignAsync(
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
            await _ticketQueryService.GetByIdAsync(
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
