using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.TicketActivity.Dtos;
using MaintenanceRequestSystem.Application.TicketActivity.Interfaces;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.TicketActivity.Services;

public sealed class TicketActivityService : ITicketActivityService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly ITicketActivityRepository _activityRepository;
    private readonly IUserRepository _userRepository;

    public TicketActivityService(
        ITicketRepository ticketRepository,
        ITicketActivityRepository activityRepository,
        IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _activityRepository = activityRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResult<TicketActivityDto>> GetPagedAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        TicketActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(ticketId, "Geçerli bir talep kimliği gereklidir.");
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");

        if (!Enum.IsDefined(currentUserRole))
        {
            throw new ForbiddenException("Desteklenmeyen kullanıcı rolü.");
        }

        ArgumentNullException.ThrowIfNull(query);
        ValidateQuery(query);
        var currentUser = await _userRepository.GetByIdAsync(
            currentUserId,
            cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException("Kullanıcı bulunamadı.");
        }

        if (!currentUser.IsActive || currentUser.Role != currentUserRole)
        {
            throw new ForbiddenException("Kullanıcı hesabı veya rolü doğrulanamadı.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException("Talep bulunamadı.");
        }

        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talep etkinliklerine erişemezsiniz.");
        }

        if (currentUserRole == UserRole.Technician &&
            ticket.AssignedTechnicianId != currentUserId)
        {
            throw new ForbiddenException(
                "Yalnızca size atanmış talep etkinliklerine erişebilirsiniz.");
        }

        var result = await _activityRepository.GetPagedAsync(
            ticketId,
            query,
            cancellationToken);
        var totalPages = result.TotalCount == 0
            ? 0
            : (int)Math.Ceiling(result.TotalCount / (double)query.PageSize);

        return new PagedResult<TicketActivityDto>(
            result.Items,
            query.PageNumber,
            query.PageSize,
            result.TotalCount,
            totalPages);
    }

    private static void ValidateQuery(TicketActivityQuery query)
    {
        if (query.PageNumber < 1)
        {
            throw new RequestValidationException("Sayfa numarası en az 1 olmalıdır.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }

        if (((long)query.PageNumber - 1L) * query.PageSize > int.MaxValue - query.PageSize)
        {
            throw new RequestValidationException(
                "İstenen sayfa numarası desteklenen sınırı aşıyor.");
        }
    }

    private static void EnsureValidId(Guid id, string message)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(message);
        }
    }
}
