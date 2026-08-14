using MaintenanceRequestSystem.Application.TicketActivity.Dtos;
using MaintenanceRequestSystem.Application.TicketActivity.Interfaces;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Infrastructure.Repositories;

public sealed class TicketActivityRepository : ITicketActivityRepository
{
    private readonly ApplicationDbContext _context;

    public TicketActivityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(IReadOnlyList<TicketActivityDto> Items, int TotalCount)>
        GetPagedAsync(
            Guid ticketId,
            TicketActivityQuery query,
            CancellationToken cancellationToken = default)
    {
        var offset = checked((query.PageNumber - 1) * query.PageSize);
        var take = checked(offset + query.PageSize);
        var ticketIdText = ticketId.ToString();
        var created = await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId)
            .Select(ticket => new TicketActivityDto(
                ticket.Id,
                "TicketCreated",
                "Talep oluşturuldu",
                $"{ticket.TicketNumber} numaralı talep oluşturuldu.",
                ticket.CreatedByUserId,
                ticket.CreatedByUser.FullName,
                ticket.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);
        var histories = await _context.TicketHistories
            .AsNoTracking()
            .Where(history => history.TicketId == ticketId)
            .OrderByDescending(history => history.CreatedAt)
            .ThenBy(history => history.Id)
            .Take(take)
            .Select(history => new TicketActivityDto(
                history.Id,
                history.Description.Contains("kategori")
                    ? "CategoryChanged"
                    : history.OldStatus == history.NewStatus ||
                      history.NewStatus == TicketStatus.Assigned
                        ? "AssignmentChanged"
                        : "StatusChanged",
                history.Description.Contains("kategori")
                    ? "Kategori değiştirildi"
                    : history.OldStatus == history.NewStatus ||
                      history.NewStatus == TicketStatus.Assigned
                        ? "Teknik personel ataması değiştirildi"
                        : "Durum değiştirildi",
                history.Description,
                history.PerformedByUserId,
                history.PerformedByUser.FullName,
                history.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);
        var comments = await _context.TicketComments
            .AsNoTracking()
            .Where(comment => comment.TicketId == ticketId)
            .OrderByDescending(comment => comment.CreatedAt)
            .ThenBy(comment => comment.Id)
            .Take(take)
            .Select(comment => new TicketActivityDto(
                comment.Id,
                "CommentAdded",
                "Yorum eklendi",
                "Talebe yeni bir yorum eklendi.",
                comment.UserId,
                comment.User.FullName,
                comment.CreatedAt,
                comment.Id,
                null))
            .ToListAsync(cancellationToken);
        var attachments = await _context.TicketAttachments
            .AsNoTracking()
            .Where(attachment => attachment.TicketId == ticketId)
            .OrderByDescending(attachment => attachment.CreatedAt)
            .ThenBy(attachment => attachment.Id)
            .Take(take)
            .Select(attachment => new TicketActivityDto(
                attachment.Id,
                "AttachmentUploaded",
                "Dosya eklendi",
                attachment.OriginalFileName,
                attachment.UploadedByUserId,
                attachment.UploadedByUser.FullName,
                attachment.CreatedAt,
                null,
                attachment.Id))
            .ToListAsync(cancellationToken);
        var priorityChanges = await _context.AuditLogs
            .AsNoTracking()
            .Where(log =>
                log.EntityName == "Ticket" &&
                log.EntityId == ticketIdText &&
                log.Action == "TicketPriorityChanged")
            .OrderByDescending(log => log.CreatedAt)
            .ThenBy(log => log.Id)
            .Take(take)
            .Select(log => new TicketActivityDto(
                log.Id,
                "PriorityChanged",
                "Öncelik değiştirildi",
                "Talep önceliği değiştirildi.",
                log.PerformedByUserId,
                log.PerformedByUser.FullName,
                log.CreatedAt,
                null,
                null))
            .ToListAsync(cancellationToken);

        var totalCount = created.Count +
            await _context.TicketHistories.CountAsync(
                history => history.TicketId == ticketId,
                cancellationToken) +
            await _context.TicketComments.CountAsync(
                comment => comment.TicketId == ticketId,
                cancellationToken) +
            await _context.TicketAttachments.CountAsync(
                attachment => attachment.TicketId == ticketId,
                cancellationToken) +
            await _context.AuditLogs.CountAsync(log =>
                log.EntityName == "Ticket" &&
                log.EntityId == ticketIdText &&
                log.Action == "TicketPriorityChanged",
                cancellationToken);
        var items = created
            .Concat(histories)
            .Concat(comments)
            .Concat(attachments)
            .Concat(priorityChanges)
            .OrderByDescending(item => item.CreatedAt)
            .ThenBy(item => item.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .ToList();

        return (items, totalCount);
    }
}
