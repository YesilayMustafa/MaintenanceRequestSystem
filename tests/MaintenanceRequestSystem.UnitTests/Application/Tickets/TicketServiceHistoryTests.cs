using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
    [Fact]
    public async Task GetHistoryAsync_ByAdmin_ReturnsTicketHistories()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();

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

        ticket.Assign(
            technician.Id,
            admin.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket,
                Histories = ticket.Histories.ToList()
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

        var result =
            await service.GetHistoryAsync(
                ticket.Id,
                admin.Id,
                UserRole.Admin);

        Assert.Single(result);

        Assert.Equal(
            "Open",
            result[0].OldStatus);

        Assert.Equal(
            "Assigned",
            result[0].NewStatus);

        Assert.Equal(
            1,
            ticketRepository.GetHistoriesCallCount);
    }

    [Fact]
    public async Task GetHistoryAsync_ByTicketCreator_ReturnsTicketHistories()
    {
        var creator = CreateUser();

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
                TicketById = ticket,
                Histories = ticket.Histories.ToList()
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

        var result =
            await service.GetHistoryAsync(
                ticket.Id,
                creator.Id,
                UserRole.Employee);

        Assert.Single(result);

        Assert.Equal(
            "Cancelled",
            result[0].NewStatus);

        Assert.Equal(
            1,
            ticketRepository.GetHistoriesCallCount);
    }

    [Fact]
    public async Task GetHistoryAsync_ByAssignedTechnician_ReturnsTicketHistories()
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
                TicketById = ticket,
                Histories = ticket.Histories.ToList()
            };

        var service =
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

        var result =
            await service.GetHistoryAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician);

        Assert.Single(result);

        Assert.Equal(
            "Assigned",
            result[0].NewStatus);
    }

    [Fact]
    public async Task GetHistoryAsync_ByDifferentEmployee_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var differentEmployee = CreateUser();

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
                    UserById = differentEmployee
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetHistoryAsync(
                ticket.Id,
                differentEmployee.Id,
                UserRole.Employee));

        Assert.Equal(
            0,
            ticketRepository.GetHistoriesCallCount);
    }
}
