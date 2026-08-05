using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
    [Fact]
    public async Task CancelAsync_ByTicketCreator_WhenOpen_CancelsAndSavesTicket()
    {
        var creator = CreateUser();
        var asset = CreateAsset();

        var ticket =
            new Ticket(
                asset.Id,
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
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
        var auditLogService =
    new FakeAuditLogService();
        var service =
            new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = creator
                },
                auditLogService);

        var result =
            await service.CancelAsync(
                ticket.Id,
                creator.Id,
                UserRole.Employee);

        Assert.Equal(
            TicketStatus.Cancelled,
            ticket.Status);

        Assert.Equal(
            "Cancelled",
            result.Status);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);

        Assert.Equal(
    1,
    auditLogService.AddCallCount);

        Assert.Equal(
            creator.Id,
            auditLogService.PerformedByUserId);

        Assert.Equal(
            "TicketCancelled",
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
    public async Task CancelAsync_ByAdmin_WhenAssigned_CancelsTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
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
                TicketPriority.High);

        SetTicketNavigationProperties(
            ticket,
            asset,
            creator);

        ticket.Assign(
            technician.Id,
            admin.Id);

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

        await service.CancelAsync(
            ticket.Id,
            admin.Id,
            UserRole.Admin);

        Assert.Equal(
            TicketStatus.Cancelled,
            ticket.Status);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelAsync_ByTicketCreator_WhenAssigned_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

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
                    UserById = creator
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CancelAsync(
                ticket.Id,
                creator.Id,
                UserRole.Employee));

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CancelAsync_ByDifferentEmployee_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var differentEmployee = CreateUser();

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = differentEmployee
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CancelAsync(
                ticket.Id,
                differentEmployee.Id,
                UserRole.Employee));

        Assert.Equal(
            TicketStatus.Open,
            ticket.Status);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

}
