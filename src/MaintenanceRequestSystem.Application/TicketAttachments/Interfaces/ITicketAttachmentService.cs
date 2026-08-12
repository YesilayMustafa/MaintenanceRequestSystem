using MaintenanceRequestSystem.Application.TicketAttachments.Dtos;
using MaintenanceRequestSystem.Application.TicketAttachments.Models;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;

public interface ITicketAttachmentService
{
    Task<IReadOnlyList<TicketAttachmentDto>> GetAllAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task<TicketAttachmentDto> UploadAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        AttachmentUpload upload,
        CancellationToken cancellationToken = default);

    Task<AttachmentDownload> DownloadAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default);
}
