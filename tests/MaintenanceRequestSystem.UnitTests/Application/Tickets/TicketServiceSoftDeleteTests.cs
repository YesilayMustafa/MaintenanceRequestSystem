using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
    [Fact]
    public async Task SoftDeleteAsync_ByAdmin_WhenCancelled_SoftDeletesTicket()
    {
        var creator = CreateUser();

        var admin =
            new User(
                "Test Yöneticisi",
                $"admin-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                UserRole.Admin,
                Guid.NewGuid());

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

        ticket.Cancel(
            creator.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };
        var auditLogService =
    new FakeAuditLogService();
        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = admin
                },
                auditLogService);

        await service.SoftDeleteAsync(
            ticket.Id,
            admin.Id,
            UserRole.Admin);

        Assert.True(
            ticket.IsDeleted);

        Assert.NotNull(
            ticket.DeletedAt);

        Assert.Equal(
            admin.Id,
            ticket.DeletedByUserId);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);

        Assert.Equal(
    1,
    auditLogService.AddCallCount);

        Assert.Equal(
            admin.Id,
            auditLogService.PerformedByUserId);

        Assert.Equal(
            "TicketSoftDeleted",
            auditLogService.Action);

        Assert.Equal(
            nameof(Ticket),
            auditLogService.EntityName);

        Assert.Equal(
            ticket.Id.ToString(),
            auditLogService.EntityId);

        Assert.NotNull(
            auditLogService.OldValues);

        Assert.NotNull(
            auditLogService.NewValues);
    }

    [Fact]
    public async Task SoftDeleteAsync_ByEmployee_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SoftDeleteAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Employee));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task SoftDeleteAsync_WhenTicketIsOpen_ThrowsArgumentException()
    {
        var creator = CreateUser();

        var admin =
            new User(
                "Test Yöneticisi",
                $"admin-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                UserRole.Admin,
                Guid.NewGuid());

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

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
                    UserById = admin
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.SoftDeleteAsync(
                ticket.Id,
                admin.Id,
                UserRole.Admin));

        Assert.False(
            ticket.IsDeleted);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

}
