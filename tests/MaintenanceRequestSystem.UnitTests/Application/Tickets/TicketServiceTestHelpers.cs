using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Services;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
    private static readonly IAuditLogService NoOpAuditLogService =
        new NullAuditLogService();

    private static TicketService CreateTicketService(
        ITicketRepository ticketRepository,
        IAssetRepository assetRepository,
        IUserRepository userRepository,
        IAuditLogService auditLogService)
    {
        return new TicketService(
            ticketRepository,
            assetRepository,
            userRepository,
            auditLogService,
            new TicketQueryService(
                ticketRepository,
                userRepository),
            new TicketCreationService(
                ticketRepository,
                assetRepository,
                userRepository));
    }

    private static CreateTicketRequest CreateRequest(
        Guid assetId)
    {
        return new CreateTicketRequest
        {
            AssetId = assetId,
            Title = "Bilgisayar açılmıyor",
            Description = "Cihaz açılmıyor.",
            Priority = TicketPriority.High
        };
    }

    private static User CreateTechnician()
    {
        return new User(
            "Test Teknisyeni",
            $"technician-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            UserRole.Technician,
            Guid.NewGuid());
    }

    private static User CreateUser()
    {
        return new User(
            "Test Kullanıcısı",
            $"user-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            UserRole.Employee,
            Guid.NewGuid());
    }

    private static Asset CreateAsset()
    {
        return new Asset(
            "Dell Latitude 5540",
            $"ASSET-{Guid.NewGuid():N}",
            AssetType.Computer,
            Guid.NewGuid(),
            "Bilgi İşlem");
    }

    private sealed class FakeTicketRepository
        : ITicketRepository
    {
        public List<Ticket> Tickets { get; } = [];

        public IReadOnlyList<TicketHistory> Histories { get; set; } =
    Array.Empty<TicketHistory>();

        public int GetHistoriesCallCount { get; private set; }

        public Task<IReadOnlyList<TicketHistory>> GetHistoriesAsync(
            Guid ticketId,
            CancellationToken cancellationToken = default)
        {
            GetHistoriesCallCount++;

            var result =
                Histories
                    .Where(history =>
                        history.TicketId == ticketId)
                    .ToList();

            return Task.FromResult<IReadOnlyList<TicketHistory>>(
                result);
        }

        public Ticket? TicketById { get; init; }

        public bool AddCalled { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<Ticket?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var ticket =
                TicketById ??
                Tickets.FirstOrDefault(
                    existingTicket =>
                        existingTicket.Id == id);

            return Task.FromResult(ticket);
        }

        public Task<(IReadOnlyList<Ticket> Items, int TotalCount)>
    GetPagedAsync(
        Guid currentUserId,
        UserRole currentUserRole,
        TicketListQuery query,
        CancellationToken cancellationToken = default)
        {
            IEnumerable<Ticket> filteredTickets =
                Tickets;

            if (currentUserRole == UserRole.Employee)
            {
                filteredTickets =
                    filteredTickets.Where(
                        ticket =>
                            ticket.CreatedByUserId ==
                            currentUserId);
            }

            var items =
                filteredTickets.ToList();

            return Task.FromResult(
                (
                    (IReadOnlyList<Ticket>)items,
                    items.Count
                ));
        }

        public Task AddAsync(
            Ticket ticket,
            CancellationToken cancellationToken = default)
        {
            AddCalled = true;
            Tickets.Add(ticket);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAuditLogService
        : IAuditLogService
    {
        public Task<PagedResult<AuditLogDto>> GetPagedAsync(
    AuditLogListQuery query,
    CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new PagedResult<AuditLogDto>(
                    Array.Empty<AuditLogDto>(),
                    query.PageNumber,
                    query.PageSize,
                    0,
                    0));
        }
        public int AddCallCount { get; private set; }

        public Guid PerformedByUserId { get; private set; }

        public string? Action { get; private set; }

        public string? EntityName { get; private set; }

        public string? EntityId { get; private set; }

        public object? OldValues { get; private set; }

        public object? NewValues { get; private set; }

        public Task AddAsync(
            Guid performedByUserId,
            string action,
            string entityName,
            string entityId,
            object? oldValues = null,
            object? newValues = null,
            CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            PerformedByUserId = performedByUserId;
            Action = action;
            EntityName = entityName;
            EntityId = entityId;
            OldValues = oldValues;
            NewValues = newValues;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeAssetRepository
        : IAssetRepository
    {
        public Asset? AssetById { get; init; }

        public Task<IReadOnlyList<Asset>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Asset> assets =
                AssetById is null
                    ? []
                    : [AssetById];

            return Task.FromResult(assets);
        }

        public Task<Asset?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(AssetById);
        }

        public Task<bool> SerialNumberExistsAsync(
            string serialNumber,
            Guid? excludedAssetId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            Asset asset,
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

    private static void SetTicketNavigationProperties(
    Ticket ticket,
    Asset asset,
    User creator)
    {
        typeof(Ticket)
            .GetProperty(nameof(Ticket.Asset))!
            .SetValue(ticket, asset);

        typeof(Ticket)
            .GetProperty(nameof(Ticket.CreatedByUser))!
            .SetValue(ticket, creator);
    }

    private sealed class FakeUserRepository
        : IUserRepository
    {
        public User? UserById { get; init; }

        public Task<IReadOnlyList<User>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> users =
                UserById is null
                    ? []
                    : [UserById];

            return Task.FromResult(users);
        }

        public Task<User?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UserById);
        }

        public Task<User?> GetByEmailAsync(
            string email,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(UserById);
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
}
