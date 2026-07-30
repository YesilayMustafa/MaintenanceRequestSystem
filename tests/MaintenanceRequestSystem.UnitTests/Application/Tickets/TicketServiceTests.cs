using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed class TicketServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsAndSavesOpenTicket()
    {
        var user = CreateUser();
        var asset = CreateAsset();

        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository
                {
                    AssetById = asset
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        var request =
            new CreateTicketRequest
            {
                AssetId = asset.Id,
                Title = "  Bilgisayar açılmıyor  ",
                Description =
                    "  Güç düğmesine basıldığında cihaz açılmıyor.  ",
                Priority = TicketPriority.High
            };

        var result =
            await service.CreateAsync(
                user.Id,
                request);

        var createdTicket =
            Assert.Single(
                ticketRepository.Tickets);

        Assert.True(ticketRepository.AddCalled);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);

        Assert.Equal(
            "Bilgisayar açılmıyor",
            createdTicket.Title);

        Assert.Equal(
            TicketStatus.Open,
            createdTicket.Status);

        Assert.Equal(
            TicketPriority.High,
            createdTicket.Priority);

        Assert.Equal(asset.Id, createdTicket.AssetId);
        Assert.Equal(user.Id, createdTicket.CreatedByUserId);
        Assert.Null(createdTicket.AssignedTechnicianId);

        Assert.Equal(
            asset.Name,
            result.AssetName);

        Assert.Equal(
            user.FullName,
            result.CreatedByFullName);

        Assert.Equal("Open", result.Status);
        Assert.Equal("High", result.Priority);
    }

    [Fact]
    public async Task CreateAsync_WithMissingUser_ThrowsKeyNotFoundException()
    {
        var service =
            new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository
                {
                    AssetById = CreateAsset()
                },
                new FakeUserRepository
                {
                    UserById = null
                });

        var request =
            CreateRequest(
                Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                request));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveUser_ThrowsForbiddenException()
    {
        var user = CreateUser();
        user.Deactivate();

        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository
                {
                    AssetById = CreateAsset()
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CreateAsync(
                user.Id,
                CreateRequest(Guid.NewGuid())));

        Assert.False(ticketRepository.AddCalled);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithMissingAsset_ThrowsKeyNotFoundException()
    {
        var user = CreateUser();

        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository
                {
                    AssetById = null
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                user.Id,
                CreateRequest(Guid.NewGuid())));

        Assert.False(ticketRepository.AddCalled);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveAsset_ThrowsValidationException()
    {
        var user = CreateUser();
        var asset = CreateAsset();

        asset.Deactivate();

        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository
                {
                    AssetById = asset
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(
                user.Id,
                CreateRequest(asset.Id)));

        Assert.False(ticketRepository.AddCalled);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingTicket_ThrowsKeyNotFoundException()
    {
        var service =
            new TicketService(
                new FakeTicketRepository
                {
                    TicketById = null
                },
                new FakeAssetRepository(),
                new FakeUserRepository());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.GetByIdAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Admin));
    }

    [Fact]
    public async Task GetByIdAsync_WhenEmployeeRequestsOtherUsersTicket_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var otherEmployee = CreateUser();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var service =
            new TicketService(
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeAssetRepository(),
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByIdAsync(
                ticket.Id,
                otherEmployee.Id,
                UserRole.Employee));
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