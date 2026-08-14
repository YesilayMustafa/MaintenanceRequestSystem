using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.TicketAttachments.Dtos;
using MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;
using MaintenanceRequestSystem.Application.TicketAttachments.Models;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.Application.TicketAttachments.Services;

public sealed class TicketAttachmentService : ITicketAttachmentService
{
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    private static readonly byte[] PngSignature =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static readonly IReadOnlyDictionary<string, string> ContentTypeByExtension =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp",
            [".pdf"] = "application/pdf"
        };

    private readonly ITicketAttachmentRepository _attachmentRepository;
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAttachmentStorage _storage;
    private readonly IAuditLogService _auditLogService;
    private readonly AttachmentSettings _settings;

    public TicketAttachmentService(
        ITicketAttachmentRepository attachmentRepository,
        ITicketRepository ticketRepository,
        IUserRepository userRepository,
        IAttachmentStorage storage,
        IAuditLogService auditLogService,
        AttachmentSettings settings)
    {
        _attachmentRepository = attachmentRepository;
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _storage = storage;
        _auditLogService = auditLogService;
        _settings = settings;
    }

    public async Task<IReadOnlyList<TicketAttachmentDto>> GetAllAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        await GetTicketAndEnsureAccessAsync(
            ticketId,
            currentUserId,
            currentUserRole,
            cancellationToken);

        var attachments = await _attachmentRepository.GetByTicketIdAsync(
            ticketId,
            cancellationToken);

        return attachments
            .Select(attachment => MapToDto(attachment))
            .ToList();
    }

    public async Task<TicketAttachmentDto> UploadAsync(
        Guid ticketId,
        Guid currentUserId,
        UserRole currentUserRole,
        AttachmentUpload upload,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(upload);

        var (ticket, currentUser) = await GetTicketAndEnsureAccessAsync(
            ticketId,
            currentUserId,
            currentUserRole,
            cancellationToken);

        if (ticket.Status is TicketStatus.Closed or TicketStatus.Cancelled)
        {
            throw new RequestValidationException(
                "Kapatılmış veya iptal edilmiş taleplere dosya eklenemez.");
        }

        var (fileName, extension, contentType) = ValidateUpload(upload);
        await EnsureValidFileSignatureAsync(
            upload.Content,
            extension,
            cancellationToken);

        var attachmentCount = await _attachmentRepository.CountByTicketIdAsync(
            ticketId,
            cancellationToken);

        if (attachmentCount >= _settings.MaxAttachmentsPerTicket)
        {
            throw new RequestValidationException(
                $"Bir talebe en fazla {_settings.MaxAttachmentsPerTicket} dosya eklenebilir.");
        }

        var storageKey = await _storage.SaveAsync(
            upload.Content,
            extension,
            cancellationToken);

        try
        {
            var attachment = new TicketAttachment(
                ticketId,
                currentUserId,
                fileName,
                storageKey,
                contentType,
                upload.SizeBytes);

            await _attachmentRepository.AddAsync(
                attachment,
                cancellationToken);

            await _auditLogService.AddAsync(
                currentUserId,
                "TicketAttachmentUploaded",
                nameof(TicketAttachment),
                attachment.Id.ToString(),
                newValues: new
                {
                    attachment.TicketId,
                    AttachmentId = attachment.Id,
                    attachment.OriginalFileName,
                    attachment.ContentType,
                    attachment.SizeBytes
                },
                cancellationToken: cancellationToken);

            await _attachmentRepository.SaveChangesAsync(cancellationToken);

            return MapToDto(attachment, currentUser);
        }
        catch
        {
            await _storage.DeleteIfExistsAsync(
                storageKey,
                CancellationToken.None);
            throw;
        }
    }

    public async Task<AttachmentDownload> DownloadAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        await GetTicketAndEnsureAccessAsync(
            ticketId,
            currentUserId,
            currentUserRole,
            cancellationToken);

        EnsureValidId(attachmentId, "Geçerli bir dosya kimliği gereklidir.");

        var attachment = await _attachmentRepository.GetByIdAsync(
            ticketId,
            attachmentId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Dosya bulunamadı.");

        var content = await _storage.OpenReadAsync(
            attachment.StorageKey,
            cancellationToken)
            ?? throw new KeyNotFoundException("Dosya içeriği bulunamadı.");

        return new AttachmentDownload(
            content,
            attachment.OriginalFileName,
            attachment.ContentType);
    }

    public async Task DeleteAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid currentUserId,
        UserRole currentUserRole,
        CancellationToken cancellationToken = default)
    {
        await GetTicketAndEnsureAccessAsync(
            ticketId,
            currentUserId,
            currentUserRole,
            cancellationToken);

        EnsureValidId(attachmentId, "Geçerli bir dosya kimliği gereklidir.");

        var attachment = await _attachmentRepository.GetByIdAsync(
            ticketId,
            attachmentId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Dosya bulunamadı.");

        if (currentUserRole != UserRole.Admin &&
            attachment.UploadedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Yalnızca dosyayı yükleyen kullanıcı veya yönetici dosyayı silebilir.");
        }

        await _auditLogService.AddAsync(
            currentUserId,
            "TicketAttachmentDeleted",
            nameof(TicketAttachment),
            attachment.Id.ToString(),
            oldValues: new
            {
                attachment.TicketId,
                AttachmentId = attachment.Id,
                attachment.OriginalFileName,
                attachment.ContentType,
                attachment.SizeBytes
            },
            cancellationToken: cancellationToken);

        _attachmentRepository.Remove(attachment);
        await _attachmentRepository.SaveChangesAsync(cancellationToken);

        // Metadata silindikten sonra idempotent cleanup yapılır; eksik dosya silmeyi engellemez.
        await _storage.DeleteIfExistsAsync(
            attachment.StorageKey,
            CancellationToken.None);
    }

    private async Task<(Ticket Ticket, User CurrentUser)>
        GetTicketAndEnsureAccessAsync(
            Guid ticketId,
            Guid currentUserId,
            UserRole currentUserRole,
            CancellationToken cancellationToken)
    {
        EnsureValidId(ticketId, "Geçerli bir talep kimliği gereklidir.");
        EnsureValidId(currentUserId, "Geçerli bir kullanıcı kimliği gereklidir.");

        if (!Enum.IsDefined(currentUserRole))
        {
            throw new ForbiddenException("Desteklenmeyen kullanıcı rolü.");
        }

        var currentUser = await _userRepository.GetByIdAsync(
            currentUserId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Kullanıcı bulunamadı.");

        if (!currentUser.IsActive || currentUser.Role != currentUserRole)
        {
            throw new ForbiddenException("Kullanıcı hesabı veya rolü doğrulanamadı.");
        }

        var ticket = await _ticketRepository.GetByIdAsync(
            ticketId,
            cancellationToken)
            ?? throw new KeyNotFoundException("Talep bulunamadı.");

        if (currentUserRole == UserRole.Employee &&
            ticket.CreatedByUserId != currentUserId)
        {
            throw new ForbiddenException(
                "Başka bir kullanıcıya ait talebin dosyalarına erişemezsiniz.");
        }

        if (currentUserRole == UserRole.Technician &&
            ticket.AssignedTechnicianId != currentUserId)
        {
            throw new ForbiddenException(
                "Yalnızca size atanmış taleplerin dosyalarına erişebilirsiniz.");
        }

        return (ticket, currentUser);
    }

    private (string FileName, string Extension, string ContentType)
        ValidateUpload(AttachmentUpload upload)
    {
        if (upload.Content is null || !upload.Content.CanRead)
        {
            throw new RequestValidationException("Okunabilir bir dosya gereklidir.");
        }

        if (upload.SizeBytes <= 0 ||
            upload.SizeBytes > _settings.MaxFileSizeBytes)
        {
            throw new RequestValidationException(
                $"Dosya boyutu 1 byte ile {_settings.MaxFileSizeBytes} byte arasında olmalıdır.");
        }

        var fileName = NormalizeFileName(upload.FileName);
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (!_settings.AllowedExtensions.Contains(extension) ||
            !ContentTypeByExtension.TryGetValue(extension, out var expectedContentType))
        {
            throw new RequestValidationException("Dosya uzantısına izin verilmiyor.");
        }

        var contentType = upload.ContentType.Trim().ToLowerInvariant();

        if (!_settings.AllowedContentTypes.Contains(contentType) ||
            !string.Equals(
                contentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new RequestValidationException(
                "Dosya içerik türü uzantıyla uyumlu değil veya izin verilmiyor.");
        }

        return (fileName, extension, contentType);
    }

    private static async Task EnsureValidFileSignatureAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw new RequestValidationException(
                "Dosya içeriği güvenli biçimde doğrulanamadı.");
        }

        var originalPosition = content.Position;
        var header = new byte[12];
        var bytesRead = 0;

        try
        {
            while (bytesRead < header.Length)
            {
                var read = await content.ReadAsync(
                    header.AsMemory(bytesRead),
                    cancellationToken);

                if (read == 0)
                {
                    break;
                }

                bytesRead += read;
            }
        }
        finally
        {
            content.Position = originalPosition;
        }

        var signature = header.AsSpan(0, bytesRead);
        var isValid = extension switch
        {
            ".jpg" or ".jpeg" =>
                signature.StartsWith(JpegSignature),
            ".png" =>
                signature.StartsWith(PngSignature),
            ".webp" =>
                signature.Length >= 12 &&
                signature[..4].SequenceEqual("RIFF"u8) &&
                signature[8..12].SequenceEqual("WEBP"u8),
            ".pdf" =>
                signature.StartsWith("%PDF-"u8),
            _ => false
        };

        if (!isValid)
        {
            throw new RequestValidationException(
                "Dosya içeriği uzantısıyla uyumlu değil.");
        }
    }

    private static string NormalizeFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new RequestValidationException("Dosya adı boş olamaz.");
        }

        var normalized = Path.GetFileName(
            fileName.Replace('\\', '/')).Trim();

        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized is "." or ".." ||
            normalized.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            normalized.Length > TicketAttachment.MaxOriginalFileNameLength)
        {
            throw new RequestValidationException("Geçerli bir dosya adı gereklidir.");
        }

        return normalized;
    }

    private static void EnsureValidId(Guid id, string message)
    {
        if (id == Guid.Empty)
        {
            throw new RequestValidationException(message);
        }
    }

    private static TicketAttachmentDto MapToDto(
        TicketAttachment attachment,
        User? uploadedByUser = null)
    {
        var uploader = uploadedByUser ?? attachment.UploadedByUser;

        return new TicketAttachmentDto(
            attachment.Id,
            attachment.TicketId,
            attachment.OriginalFileName,
            attachment.ContentType,
            attachment.SizeBytes,
            attachment.UploadedByUserId,
            uploader.FullName,
            attachment.CreatedAt);
    }
}
