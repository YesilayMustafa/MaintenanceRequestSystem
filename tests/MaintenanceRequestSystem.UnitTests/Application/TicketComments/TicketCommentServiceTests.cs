using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.TicketComments.Dtos;
using MaintenanceRequestSystem.Application.TicketComments.Interfaces;
using MaintenanceRequestSystem.Application.TicketComments.Services;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.TicketComments;

public sealed class TicketCommentServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsAndSavesComment()
    {
        var user = CreateUser();
        var ticket = CreateTicket(user.Id);

        var commentRepository =
            new FakeTicketCommentRepository();

        var service =
            new TicketCommentService(
                commentRepository,
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        var request =
            new CreateTicketCommentRequest
            {
                Content =
                    "  Güç adaptörü kontrol edildi.  "
            };

        var result =
            await service.CreateAsync(
                ticket.Id,
                user.Id,
                UserRole.Employee,
                request);

        var createdComment =
            Assert.Single(
                commentRepository.Comments);

        Assert.True(commentRepository.AddCalled);

        Assert.Equal(
            1,
            commentRepository.SaveChangesCallCount);

        Assert.Equal(
            "Güç adaptörü kontrol edildi.",
            createdComment.Content);

        Assert.Equal(ticket.Id, result.TicketId);
        Assert.Equal(user.Id, result.UserId);
        Assert.Equal(user.FullName, result.UserFullName);
        Assert.Equal("Employee", result.UserRole);
    }

    [Fact]
    public async Task CreateAsync_WithMissingTicket_ThrowsKeyNotFoundException()
    {
        var user = CreateUser();

        var commentRepository =
            new FakeTicketCommentRepository();

        var service =
            new TicketCommentService(
                commentRepository,
                new FakeTicketRepository
                {
                    TicketById = null
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                Guid.NewGuid(),
                user.Id,
                UserRole.Employee,
                CreateRequest()));

        Assert.False(commentRepository.AddCalled);

        Assert.Equal(
            0,
            commentRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WhenEmployeeUsesOtherUsersTicket_ThrowsForbiddenException()
    {
        var ticketOwner = CreateUser();
        var otherEmployee = CreateUser();

        var ticket =
            CreateTicket(ticketOwner.Id);

        var commentRepository =
            new FakeTicketCommentRepository();

        var service =
            new TicketCommentService(
                commentRepository,
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository
                {
                    UserById = otherEmployee
                });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CreateAsync(
                ticket.Id,
                otherEmployee.Id,
                UserRole.Employee,
                CreateRequest()));

        Assert.False(commentRepository.AddCalled);
    }

    [Fact]
    public async Task GetByTicketIdAsync_WithUnsupportedRole_ThrowsForbiddenException()
    {
        var service =
            new TicketCommentService(
                new FakeTicketCommentRepository(),
                new FakeTicketRepository(),
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByTicketIdAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                (UserRole)999));
    }

    [Fact]
    public async Task CreateAsync_WithMissingUser_ThrowsKeyNotFoundException()
    {
        var ticket =
            CreateTicket(Guid.NewGuid());

        var commentRepository =
            new FakeTicketCommentRepository();

        var service =
            new TicketCommentService(
                commentRepository,
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository
                {
                    UserById = null
                });

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(
                ticket.Id,
                Guid.NewGuid(),
                UserRole.Admin,
                CreateRequest()));

        Assert.False(commentRepository.AddCalled);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveUser_ThrowsForbiddenException()
    {
        var user = CreateUser();
        user.Deactivate();

        var ticket =
            CreateTicket(user.Id);

        var commentRepository =
            new FakeTicketCommentRepository();

        var service =
            new TicketCommentService(
                commentRepository,
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository
                {
                    UserById = user
                });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CreateAsync(
                ticket.Id,
                user.Id,
                UserRole.Employee,
                CreateRequest()));

        Assert.False(commentRepository.AddCalled);
    }

    [Fact]
    public async Task GetByTicketIdAsync_WhenEmployeeUsesOtherUsersTicket_ThrowsForbiddenException()
    {
        var ticketOwner = CreateUser();
        var otherEmployee = CreateUser();

        var ticket =
            CreateTicket(ticketOwner.Id);

        var service =
            new TicketCommentService(
                new FakeTicketCommentRepository(),
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByTicketIdAsync(
                ticket.Id,
                otherEmployee.Id,
                UserRole.Employee));
    }

    [Fact]
    public async Task GetByTicketIdAsync_WhenTechnicianUsesOtherTechniciansTicket_ThrowsForbiddenException()
    {
        var ticketOwner = CreateUser();
        var assignedTechnician = CreateTechnician();
        var otherTechnician = CreateTechnician();

        var ticket =
            CreateTicket(ticketOwner.Id);

        ticket.Assign(
            assignedTechnician.Id,
            Guid.NewGuid());

        var service =
            new TicketCommentService(
                new FakeTicketCommentRepository(),
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByTicketIdAsync(
                ticket.Id,
                otherTechnician.Id,
                UserRole.Technician));
    }

    [Fact]
    public async Task CreateAsync_WhenTechnicianUsesOtherTechniciansTicket_ThrowsForbiddenException()
    {
        var ticketOwner = CreateUser();
        var assignedTechnician = CreateTechnician();
        var otherTechnician = CreateTechnician();

        var ticket =
            CreateTicket(ticketOwner.Id);

        ticket.Assign(
            assignedTechnician.Id,
            Guid.NewGuid());

        var commentRepository =
            new FakeTicketCommentRepository();

        var service =
            new TicketCommentService(
                commentRepository,
                new FakeTicketRepository
                {
                    TicketById = ticket
                },
                new FakeUserRepository
                {
                    UserById = otherTechnician
                });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CreateAsync(
                ticket.Id,
                otherTechnician.Id,
                UserRole.Technician,
                CreateRequest()));

        Assert.False(commentRepository.AddCalled);
    }

    private static CreateTicketCommentRequest CreateRequest()
    {
        return new CreateTicketCommentRequest
        {
            Content = "Integration dışı test yorumu."
        };
    }

    private static User CreateUser()
    {
        return new User(
            "Test Kullanıcısı",
            $"comment-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            UserRole.Employee,
            Guid.NewGuid());
    }

    private static User CreateTechnician()
    {
        return new User(
            "Test Teknik Personeli",
            $"technician-{Guid.NewGuid():N}@example.com",
            "test-password-hash",
            UserRole.Technician,
            Guid.NewGuid());
    }

    private static Ticket CreateTicket(
        Guid createdByUserId)
    {
        return new Ticket(
            Guid.NewGuid(),
            createdByUserId,
            "Bilgisayar açılmıyor",
            "Cihaz güç düğmesine basıldığında açılmıyor.",
            TicketPriority.High);
    }

    private sealed class FakeTicketCommentRepository
        : ITicketCommentRepository
    {
        public List<TicketComment> Comments { get; } = [];

        public bool AddCalled { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<TicketComment>>
            GetByTicketIdAsync(
                Guid ticketId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<TicketComment> comments =
                Comments
                    .Where(comment =>
                        comment.TicketId == ticketId)
                    .ToList();

            return Task.FromResult(comments);
        }

        public Task AddAsync(
            TicketComment comment,
            CancellationToken cancellationToken = default)
        {
            AddCalled = true;
            Comments.Add(comment);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeTicketRepository
        : ITicketRepository
    {

        public Task<IReadOnlyList<TicketHistory>> GetHistoriesAsync(
    Guid ticketId,
    CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<TicketHistory>>(
                Array.Empty<TicketHistory>());
        }
        public Ticket? TicketById { get; init; }

        public Task<(
            IReadOnlyList<Ticket> Items,
            int TotalCount)> GetPagedAsync(
                Guid currentUserId,
                UserRole currentUserRole,
                TicketListQuery query,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                (
                    (IReadOnlyList<Ticket>)[],
                    0
                ));
        }

        public Task<Ticket?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(TicketById);
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
