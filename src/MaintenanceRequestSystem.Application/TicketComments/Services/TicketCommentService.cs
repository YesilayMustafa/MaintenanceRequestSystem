using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Application.TicketComments.Interfaces;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Interfaces;
using MaintenanceRequestSystem.Application.Notifications.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.TicketComments.Services;

public sealed class TicketCommentService
    : ITicketCommentService
{
    private readonly ITicketCommentRepository
        _commentRepository;

    private readonly ITicketRepository
        _ticketRepository;

    private readonly IUserRepository
        _userRepository;

    private readonly INotificationWriter
        _notificationWriter;

    public TicketCommentService(
        ITicketCommentRepository commentRepository,
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        INotificationWriter? notificationWriter = null)
    {
        _commentRepository = commentRepository;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _notificationWriter = notificationWriter ?? new NullNotificationWriter();
    }

    public async Task<IReadOnlyList<TicketCommentDto>>
        GetByTicketIdAsync(
            Guid ticketId,
            Guid currentUserId,
            UserRole currentUserRole,
            CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            ticketId,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureValidRole(currentUserRole);

        var ticket =
            await GetTicketAndEnsureAccessAsync(
                ticketId,
                currentUserId,
                currentUserRole,
                cancellationToken);

        var comments =
            await _commentRepository.GetByTicketIdAsync(
                ticket.Id,
                cancellationToken);

        return comments
            .Select(comment => MapToDto(comment))
            .ToList();
    }

    public async Task<TicketCommentDto> CreateAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        CreateTicketCommentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureValidId(
            ticketId,
            "Geçerli bir talep kimliği gereklidir.");

        EnsureValidId(
            currentUserId,
            "Geçerli bir kullanıcı kimliği gereklidir.");

        EnsureValidRole(currentUserRole);
        ArgumentNullException.ThrowIfNull(request);

        var ticket =
            await GetTicketAndEnsureAccessAsync(
                ticketId,
                currentUserId,
                currentUserRole,
                cancellationToken);

        if (ticket.Status is
            TicketStatus.Closed or
            TicketStatus.Cancelled)
        {
            throw new RequestValidationException(
                "Kapatılmış veya iptal edilmiş taleplere yorum eklenemez.");
        }

        var user =
            await _userRepository.GetByIdAsync(
                currentUserId,
                cancellationToken);

        if (user is null)
        {
            throw new KeyNotFoundException(
                "Yorum yapan kullanıcı bulunamadı.");
        }

        if (!user.IsActive)
        {
            throw new ForbiddenException(
                "Pasif kullanıcılar yorum ekleyemez.");
        }

        var comment = new TicketComment(
            ticketId,
            currentUserId,
            request.Content);

        await _commentRepository.AddAsync(
            comment,
            cancellationToken);

        var recipients = new List<Guid> { ticket.CreatedByUserId };

        if (ticket.AssignedTechnicianId.HasValue)
        {
            recipients.Add(ticket.AssignedTechnicianId.Value);
        }

        await _notificationWriter.AddAsync(
            currentUserId,
            recipients,
            NotificationType.TicketCommentAdded,
            "Talebe yeni yorum eklendi",
            $"{ticket.TicketNumber} numaralı talebe yeni bir yorum eklendi.",
            ticket.Id,
            cancellationToken);

        await _commentRepository.SaveChangesAsync(
            cancellationToken);

        return MapToDto(comment, user);
    }

    private async Task<Ticket>
        GetTicketAndEnsureAccessAsync(
            Guid ticketId,
            Guid currentUserId,
            UserRole currentUserRole,
            CancellationToken cancellationToken)
    {
        var ticket =
            await _ticketRepository.GetByIdAsync(
                ticketId,
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
                "Başka bir kullanıcıya ait talebin yorumlarına erişemezsiniz.");
        }

        if (currentUserRole == UserRole.Technician &&
            ticket.AssignedTechnicianId != currentUserId)
        {
            throw new ForbiddenException(
                "Yalnızca size atanmış taleplerin yorumlarına erişebilirsiniz.");
        }

        return ticket;
    }

    private static void EnsureValidId(
        Guid id,
        string errorMessage)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(
                errorMessage);
        }
    }

    private static void EnsureValidRole(
        UserRole role)
    {
        if (!Enum.IsDefined(
                typeof(UserRole),
                role))
        {
            throw new ForbiddenException(
                "Desteklenmeyen kullanıcı rolü.");
        }
    }

    private static TicketCommentDto MapToDto(
        TicketComment comment,
        User? user = null)
    {
        var commentUser =
            user ?? comment.User;

        return new TicketCommentDto(
            comment.Id,
            comment.TicketId,
            comment.UserId,
            commentUser.FullName,
            commentUser.Role.ToString(),
            comment.Content,
            comment.CreatedAt);
    }
}
