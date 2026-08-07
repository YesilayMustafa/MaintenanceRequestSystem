using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
    [Fact]
    public async Task ChangePriorityAsync_ByAdmin_ChangesAndSavesPriority()
    {
        var auditLogService =new FakeAuditLogService();
        var creator = CreateUser();
        var asset = CreateAsset();

        var admin =
            new User(
                "Test Yöneticisi",
                $"admin-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                UserRole.Admin,
                Guid.NewGuid());

        var ticket =
            new Ticket(
                asset.Id,
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.Medium);

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
            CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = admin
                },
                auditLogService);

        var result =
            await service.ChangePriorityAsync(
                ticket.Id,
                admin.Id,
                UserRole.Admin,
                new ChangeTicketPriorityRequest
                {
                    Priority = TicketPriority.Critical
                });



        Assert.Equal(
            1,
            auditLogService.AddCallCount);

        Assert.Equal(
            "TicketPriorityChanged",
            auditLogService.Action);

        Assert.Equal(
            nameof(Ticket),
            auditLogService.EntityName);

        Assert.Equal(
            ticket.Id.ToString(),
            auditLogService.EntityId);
    }

    [Fact]
    public async Task ChangePriorityAsync_ByEmployee_ThrowsForbiddenException()
    {
        var service =
CreateTicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ChangePriorityAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Employee,
                new ChangeTicketPriorityRequest
                {
                    Priority = TicketPriority.High
                }));
    }

    [Fact]
    public async Task ChangePriorityAsync_ByInactiveAdmin_ThrowsForbiddenException()
    {
        var creator = CreateUser();

        var admin =
            new User(
                "Test Yöneticisi",
                $"admin-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                UserRole.Admin,
                Guid.NewGuid());

        admin.Deactivate();

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
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = admin
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ChangePriorityAsync(
                ticket.Id,
                admin.Id,
                UserRole.Admin,
                new ChangeTicketPriorityRequest
                {
                    Priority = TicketPriority.Critical
                }));

        Assert.Equal(
            TicketPriority.Medium,
            ticket.Priority);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ChangePriorityAsync_WithSamePriority_ThrowsArgumentException()
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
                TicketPriority.High);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = admin
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ChangePriorityAsync(
                ticket.Id,
                admin.Id,
                UserRole.Admin,
                new ChangeTicketPriorityRequest
                {
                    Priority = TicketPriority.High
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

}
