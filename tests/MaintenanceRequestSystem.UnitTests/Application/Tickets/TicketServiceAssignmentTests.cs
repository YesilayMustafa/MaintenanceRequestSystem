using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
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
        var auditLogService =
            new FakeAuditLogService();

        var service =
            CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = secondTechnician
                },
                auditLogService);

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

        Assert.Equal(
    1,
    auditLogService.AddCallCount);

        Assert.Equal(
            "TicketReassigned",
            auditLogService.Action);

        Assert.Equal(
            nameof(Ticket),
            auditLogService.EntityName);

        Assert.Equal(
            ticket.Id.ToString(),
            auditLogService.EntityId);
    }

    [Fact]
    public async Task ReassignAsync_WhenCurrentUserIsNotAdmin_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
    public async Task AssignAsync_WhenTicketDoesNotExist_ThrowsKeyNotFoundException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = null
                },
                NoOpAuditLogService);

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
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = employee
                },
                NoOpAuditLogService);

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
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
        var auditLogService =
    new FakeAuditLogService();
        var service =
            CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                auditLogService);



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

        Assert.Equal(
    1,
    auditLogService.AddCallCount);

        Assert.Equal(
            "TicketAssigned",
            auditLogService.Action);

        Assert.Equal(
            nameof(Ticket),
            auditLogService.EntityName);

        Assert.Equal(
            ticket.Id.ToString(),
            auditLogService.EntityId);
    }

    [Fact]
    public async Task AssignAsync_WhenCurrentUserIsNotAdmin_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
CreateTicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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

}
