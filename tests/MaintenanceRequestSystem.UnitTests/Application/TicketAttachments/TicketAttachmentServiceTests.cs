using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.TicketAttachments.Interfaces;
using MaintenanceRequestSystem.Application.TicketAttachments.Models;
using MaintenanceRequestSystem.Application.TicketAttachments.Services;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.TicketAttachments;

public sealed class TicketAttachmentServiceTests
{
    [Fact]
    public async Task UploadAsync_WhenMetadataSaveFails_CleansStoredFile()
    {
        var user = new User(
            "Test Çalışanı",
            "attachment-test@example.com",
            "password-hash",
            UserRole.Employee,
            Guid.NewGuid());
        var ticket = new Ticket(
            "REQ-2026-000001",
            Guid.NewGuid(),
            user.Id,
            "Dosya testi",
            "Metadata kayıt hatası cleanup testi.",
            TicketPriority.Medium);
        var storage = new FakeAttachmentStorage();
        var service = new TicketAttachmentService(
            new FailingAttachmentRepository(),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(user),
            storage,
            new FakeAuditLogService(),
            new AttachmentSettings());

        await using var content = new MemoryStream(
            [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31]);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(
                ticket.Id,
                user.Id,
                UserRole.Employee,
                new AttachmentUpload(
                    content,
                    "evidence.pdf",
                    "application/pdf",
                    content.Length)));

        Assert.Equal(1, storage.DeleteCallCount);
        Assert.Equal(storage.StorageKey, storage.DeletedStorageKey);
    }

    [Fact]
    public async Task UploadAsync_WhenFileSignatureIsInvalid_RejectsBeforeStorage()
    {
        var user = new User(
            "Test Çalışanı",
            "attachment-signature@example.com",
            "password-hash",
            UserRole.Employee,
            Guid.NewGuid());
        var ticket = new Ticket(
            "REQ-2026-000002",
            Guid.NewGuid(),
            user.Id,
            "Dosya imza testi",
            "Dosya imzası doğrulama testi.",
            TicketPriority.Medium);
        var storage = new FakeAttachmentStorage();
        var service = new TicketAttachmentService(
            new FailingAttachmentRepository(),
            new FakeTicketRepository(ticket),
            new FakeUserRepository(user),
            storage,
            new FakeAuditLogService(),
            new AttachmentSettings());
        await using var content = new MemoryStream([1, 2, 3]);

        await Assert.ThrowsAsync<RequestValidationException>(() =>
            service.UploadAsync(
                ticket.Id,
                user.Id,
                UserRole.Employee,
                new AttachmentUpload(
                    content,
                    "fake.pdf",
                    "application/pdf",
                    content.Length)));

        Assert.Equal(0, storage.SaveCallCount);
    }

    private sealed class FakeAttachmentStorage : IAttachmentStorage
    {
        public string StorageKey { get; } = $"{Guid.NewGuid():N}.pdf";

        public int DeleteCallCount { get; private set; }

        public int SaveCallCount { get; private set; }

        public string? DeletedStorageKey { get; private set; }

        public Task<string> SaveAsync(
            Stream content,
            string extension,
            CancellationToken cancellationToken = default)
        {
            SaveCallCount++;
            return Task.FromResult(StorageKey);
        }

        public Task<Stream?> OpenReadAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }

        public Task DeleteIfExistsAsync(
            string storageKey,
            CancellationToken cancellationToken = default)
        {
            DeleteCallCount++;
            DeletedStorageKey = storageKey;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingAttachmentRepository
        : ITicketAttachmentRepository
    {
        public Task<IReadOnlyList<TicketAttachment>> GetByTicketIdAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TicketAttachment>>([]);
        }

        public Task<TicketAttachment?> GetByIdAsync(
            Guid ticketId,
            Guid attachmentId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<TicketAttachment?>(null);
        }

        public Task<int> CountByTicketIdAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }

        public Task AddAsync(
            TicketAttachment attachment,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(TicketAttachment attachment)
        {
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            throw new InvalidOperationException("Metadata save failed.");
        }
    }

    private sealed class FakeTicketRepository : ITicketRepository
    {
        private readonly Ticket _ticket;

        public FakeTicketRepository(Ticket ticket)
        {
            _ticket = ticket;
        }

        public Task<Ticket?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Ticket?>(_ticket.Id == id ? _ticket : null);
        }

        public Task AddAsync(
            Ticket ticket,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<(IReadOnlyList<Ticket> Items, int TotalCount)> GetPagedAsync(
            Guid currentUserId,
            UserRole currentUserRole,
            TicketListQuery query,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<(IReadOnlyList<Ticket>, int)>(([], 0));
        }

        public Task<IReadOnlyList<TicketHistory>> GetHistoriesAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TicketHistory>>([]);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly User _user;

        public FakeUserRepository(User user)
        {
            _user = user;
        }

        public Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<User>>([_user]);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(_user.Id == id ? _user : null);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(null);
        }

        public Task<bool> EmailExistsAsync(
            string email,
            Guid? excludedUserId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            User user,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public Task<PagedResult<AuditLogDto>> GetPagedAsync(
            AuditLogListQuery query,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task AddAsync(
            Guid performedByUserId,
            string action,
            string entityName,
            string entityId,
            object? oldValues = null,
            object? newValues = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
