using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.Tickets.Services;

public sealed partial class TicketService
{
    /// <summary>
    /// Ticket listesini rol bazlı kapsam, filtre, sıralama ve sayfalama ile getirir.
    /// </summary>
    public async Task<PagedResult<TicketDto>> GetPagedAsync(
    Guid currentUserId,
    UserRole currentUserRole,
    TicketListQuery query,
    CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        ArgumentNullException.ThrowIfNull(query);

        ValidateListQuery(
            currentUserRole,
            query);

        var result =
            await _ticketRepository.GetPagedAsync(
                currentUserId,
                currentUserRole,
                query,
                cancellationToken);

        var items =
            result.Items
                .Select(ticket => MapToDto(ticket))
                .ToList();

        var totalPages =
            result.TotalCount == 0
                ? 0
                : (int)Math.Ceiling(
                    result.TotalCount /
                    (double)query.PageSize);

        return new PagedResult<TicketDto>(
            items,
            query.PageNumber,
            query.PageSize,
            result.TotalCount,
            totalPages);
    }

    /// <summary>
    /// Talep sahibi, atanmış teknik personel veya Admin için
    /// talebin durum geçmişini getirir.
    /// </summary>
    public async Task<IReadOnlyList<TicketHistoryDto>> GetHistoryAsync(
        Guid id,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

        var currentUser =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (currentUser is null)
        {
            throw new KeyNotFoundException(
                "Kullanıcı bulunamadı.");
        }

        if (!currentUser.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar talep geçmişini görüntüleyemez.");
        }

        if (currentUser.Role != currentUserRole)
        {
            throw new ForbiddenException(
                "Kullanıcı rolü doğrulanamadı.");
        }

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talebin geçmişini görüntüleyemezsiniz.");
        }

        if (currentUserRole == UserRole.Technician &&
            ticket.AssignedTechnicianId != currentUserId)
        {
            throw new ForbiddenException(
                "Yalnızca size atanmış taleplerin geçmişini görüntüleyebilirsiniz.");
        }

        var histories =
            await _ticketRepository.GetHistoriesAsync(
                id,
                cancellationToken);

        return histories
            .Select(history =>
                new TicketHistoryDto
                {
                    Id = history.Id,

                    PerformedByUserId =
                        history.PerformedByUserId,

                    OldStatus =
                        history.OldStatus?.ToString(),

                    NewStatus =
                        history.NewStatus.ToString(),

                    Description =
                        history.Description,

                    OccurredAt =
                        history.CreatedAt
                })
            .ToList();
    }

    /// <summary>
    /// Rol bazlı erişim kuralını uygulayarak ticket detayını getirir.
    /// </summary>
    public async Task<TicketDto> GetByIdAsync(
    Guid id,
    Guid currentUserId,
    UserRole currentUserRole,
    CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            id,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureSupportedRole(currentUserRole);

        var ticket =
            await _ticketRepository.GetByIdAsync(
                id,
                cancellationToken);

        if (ticket is null)
        {
            throw new KeyNotFoundException(
                "Talep bulunamadı.");
        }

        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talebi görüntüleyemezsiniz.");
        }

        return MapToDto(ticket);
    }

    private static void ValidateListQuery(
    UserRole currentUserRole,
    TicketListQuery query)
    {
        if (!Enum.IsDefined(
                typeof(UserRole),
                currentUserRole))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }

        if (query.PageNumber < 1)
        {
            throw new RequestValidationException(
                "Sayfa numarası en az 1 olmalıdır.");
        }

        if (query.PageSize is < 1 or > 100)
        {
            throw new RequestValidationException(
                "Sayfa boyutu 1 ile 100 arasında olmalıdır.");
        }
        var offset =
    ((long)query.PageNumber - 1L) *
    query.PageSize;

        if (offset > int.MaxValue)
        {
            throw new RequestValidationException(
                "İstenen sayfa numarası desteklenen sınırı aşıyor.");
        }

        if (query.Status.HasValue &&
    !Enum.IsDefined(
        typeof(TicketStatus),
        query.Status.Value))
        {
            throw new RequestValidationException(
                "Geçersiz talep durumu.");
        }
        if (query.Priority.HasValue &&
            !Enum.IsDefined(
                typeof(TicketPriority),
                query.Priority.Value))
        {
            throw new RequestValidationException(
                "Geçersiz talep önceliği.");
        }

        if (query.AssetId == Guid.Empty)
        {
            throw new RequestValidationException(
                "Geçerli bir cihaz kimliği gereklidir.");
        }

        var allowedSortFields =
            new[]
            {
            "createdat",
            "title",
            "priority",
            "status"
            };

        var normalizedSortBy =
            query.SortBy.Trim().ToLowerInvariant();

        if (!allowedSortFields.Contains(
                normalizedSortBy))
        {
            throw new RequestValidationException(
                "Sıralama alanı createdAt, title, priority veya status olmalıdır.");
        }
    }
}
