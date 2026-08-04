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
    public async Task GetByIdAsync_WithUnsupportedRole_ThrowsForbiddenException()
    {
        var service =
            new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByIdAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                (UserRole)999));
    }

    [Fact]
    public async Task GetPagedAsync_WithUndefinedStatus_ThrowsValidationException()
    {
        var service =
            new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository());

        var query =
            new TicketListQuery
            {
                PageNumber = 1,
                PageSize = 10,
                Status = (TicketStatus)999
            };

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetPagedAsync(
                Guid.NewGuid(),
                UserRole.Admin,
                query));
    }

    [Fact]
    public async Task ReassignAsync_WithValidRequest_ReassignsAndSavesTicket()
    {
        var creator = CreateUser();
        var firstTechnician = CreateTechnician();
        var secondTechnician = CreateTechnician();
        var asset = CreateAsset();
        var adminId = Guid.NewGuid();

        var ticket =
            new Ticket(
                asset.Id,
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        SetTicketNavigationProperties(
            ticket,
            asset,
            creator);

        ticket.Assign(
            firstTechnician.Id,
            adminId);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = secondTechnician
                });

        var result =
            await service.ReassignAsync(
                ticket.Id,
                adminId,
                UserRole.Admin,
                new AssignTicketRequest
                {
                    TechnicianId = secondTechnician.Id
                });

        Assert.Equal(
            secondTechnician.Id,
            ticket.AssignedTechnicianId);

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);

        Assert.Equal(
            secondTechnician.Id,
            result.AssignedTechnicianId);

        Assert.Equal(
            secondTechnician.FullName,
            result.AssignedTechnicianFullName);

        Assert.Equal(
            2,
            ticket.Histories.Count);
    }

    [Fact]
    public async Task ReassignAsync_WhenCurrentUserIsNotAdmin_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ReassignAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Technician,
                new AssignTicketRequest
                {
                    TechnicianId = Guid.NewGuid()
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ReassignAsync_WhenTicketIsOpen_ThrowsArgumentException()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ReassignAsync(
                ticket.Id,
                Guid.NewGuid(),
                UserRole.Admin,
                new AssignTicketRequest
                {
                    TechnicianId = technician.Id
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task GetPagedAsync_WithOverflowingOffset_ThrowsValidationException()
    {
        var service =
            new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository());

        var query =
            new TicketListQuery
            {
                PageNumber = int.MaxValue,
                PageSize = 100
            };

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.GetPagedAsync(
                Guid.NewGuid(),
                UserRole.Admin,
                query));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyAssetId_ThrowsValidationException()
    {
        var service =
            new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository());

        var request =
            new CreateTicketRequest
            {
                AssetId = Guid.Empty,
                Title = "Test talebi",
                Description = "Test açıklaması",
                Priority = TicketPriority.Medium
            };

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                request));
    }

    [Fact]
    public async Task AssignAsync_WhenTicketDoesNotExist_ThrowsKeyNotFoundException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository());

        var request =
            new AssignTicketRequest
            {
                TechnicianId = Guid.NewGuid()
            };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AssignAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Admin,
                request));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AssignAsync_WhenTechnicianDoesNotExist_ThrowsKeyNotFoundException()
    {
        var creator =
            CreateUser();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = null
                });

        var request =
            new AssignTicketRequest
            {
                TechnicianId = Guid.NewGuid()
            };

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.AssignAsync(
                ticket.Id,
                Guid.NewGuid(),
                UserRole.Admin,
                request));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AssignAsync_WhenTargetUserIsNotTechnician_ThrowsValidationException()
    {
        var creator = CreateUser();
        var employee = CreateUser();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = employee
                });

        var request =
            new AssignTicketRequest
            {
                TechnicianId = employee.Id
            };

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.AssignAsync(
                ticket.Id,
                Guid.NewGuid(),
                UserRole.Admin,
                request));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AssignAsync_WhenTechnicianIsInactive_ThrowsValidationException()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
        technician.Deactivate();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                });

        var request =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.AssignAsync(
                ticket.Id,
                Guid.NewGuid(),
                UserRole.Admin,
                request));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task AssignAsync_WithValidRequest_AssignsAndSavesTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
        var asset = CreateAsset();
        var adminId = Guid.NewGuid();

        var ticket =
            new Ticket(
                asset.Id,
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        SetTicketNavigationProperties(
            ticket,
            asset,
            creator);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                });

        var request =
            new AssignTicketRequest
            {
                TechnicianId = technician.Id
            };

        var result =
            await service.AssignAsync(
                ticket.Id,
                adminId,
                UserRole.Admin,
                request);

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.Equal(
            technician.Id,
            ticket.AssignedTechnicianId);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);

        Assert.Equal(
            "Assigned",
            result.Status);

        Assert.Equal(
            technician.Id,
            result.AssignedTechnicianId);

        Assert.Equal(
            technician.FullName,
            result.AssignedTechnicianFullName);

        var history =
            Assert.Single(ticket.Histories);

        Assert.Equal(
            adminId,
            history.PerformedByUserId);

        Assert.Equal(
            TicketStatus.Open,
            history.OldStatus);

        Assert.Equal(
            TicketStatus.Assigned,
            history.NewStatus);
    }

    [Fact]
    public async Task AssignAsync_WhenCurrentUserIsNotAdmin_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository());

        var request =
            new AssignTicketRequest
            {
                TechnicianId = Guid.NewGuid()
            };

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.AssignAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Employee,
                request));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
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