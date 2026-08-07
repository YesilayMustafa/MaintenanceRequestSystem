using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
{
    [Fact]
    public async Task StartProgressAsync_ByAssignedTechnician_StartsAndSavesTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
        var asset = CreateAsset();

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
            technician.Id,
            Guid.NewGuid());

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = technician
                });

        var result =
            await service.StartProgressAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician);

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Equal(
            "InProgress",
            result.Status);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);

        Assert.Equal(
            2,
            ticket.Histories.Count);
    }

    [Fact]
    public async Task StartProgressAsync_ByEmployee_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.StartProgressAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Employee));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task StartProgressAsync_ByInactiveTechnician_ThrowsForbiddenException()
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

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = technician
                });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.StartProgressAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician));

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task StartProgressAsync_ByDifferentTechnician_ThrowsArgumentException()
    {
        var creator = CreateUser();
        var assignedTechnician = CreateTechnician();
        var differentTechnician = CreateTechnician();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Bilgisayar açılmıyor",
                "Cihaz açılmıyor.",
                TicketPriority.High);

        ticket.Assign(
            assignedTechnician.Id,
            Guid.NewGuid());

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = differentTechnician
                });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.StartProgressAsync(
                ticket.Id,
                differentTechnician.Id,
                UserRole.Technician));

        Assert.Equal(
            TicketStatus.Assigned,
            ticket.Status);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task PutOnHoldAsync_ByAssignedTechnician_PutsTicketOnHold()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
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

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = technician
                });

        var result =
            await service.PutOnHoldAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician,
                new PutTicketOnHoldRequest
                {
                    Reason = "  Yedek parça bekleniyor.  "
                });

        Assert.Equal(
            TicketStatus.Waiting,
            ticket.Status);

        Assert.Equal(
            "Yedek parça bekleniyor.",
            result.WaitingReason);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task PutOnHoldAsync_ByEmployee_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.PutOnHoldAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Employee,
                new PutTicketOnHoldRequest
                {
                    Reason = "Test"
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResumeAsync_ByAssignedTechnician_ResumesTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
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

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            technician.Id);

        ticket.PutOnHold(
            "Yedek parça bekleniyor.",
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = technician
                });

        var result =
            await service.ResumeAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician);

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Null(
            result.WaitingReason);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResumeAsync_ByDifferentTechnician_ThrowsArgumentException()
    {
        var creator = CreateUser();
        var assignedTechnician = CreateTechnician();
        var differentTechnician = CreateTechnician();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        ticket.Assign(
            assignedTechnician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            assignedTechnician.Id);

        ticket.PutOnHold(
            "Yedek parça bekleniyor.",
            assignedTechnician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = differentTechnician
                });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ResumeAsync(
                ticket.Id,
                differentTechnician.Id,
                UserRole.Technician));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResolveAsync_ByAssignedTechnician_ResolvesAndSavesTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
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

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = technician
                });

        var result =
            await service.ResolveAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "  Ağ yapılandırması düzeltildi.  "
                });

        Assert.Equal(
            TicketStatus.Resolved,
            ticket.Status);

        Assert.Equal(
            "Resolved",
            result.Status);

        Assert.Equal(
            "Ağ yapılandırması düzeltildi.",
            result.ResolutionDescription);

        Assert.NotNull(
            result.ResolvedAt);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResolveAsync_ByEmployee_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository());

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ResolveAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Employee,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Sorun çözüldü."
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResolveAsync_ByInactiveTechnician_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
        technician.Deactivate();

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

        ticket.StartProgress(
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = technician
                });

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ResolveAsync(
                ticket.Id,
                technician.Id,
                UserRole.Technician,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Sorun çözüldü."
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ResolveAsync_ByDifferentTechnician_ThrowsArgumentException()
    {
        var creator = CreateUser();
        var assignedTechnician = CreateTechnician();
        var differentTechnician = CreateTechnician();

        var ticket =
            new Ticket(
                Guid.NewGuid(),
                creator.Id,
                "Sunucu bağlantı sorunu",
                "Sunucuya bağlantı kurulamıyor.",
                TicketPriority.High);

        ticket.Assign(
            assignedTechnician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            assignedTechnician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketTechnicianLifecycleService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = differentTechnician
                });

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.ResolveAsync(
                ticket.Id,
                differentTechnician.Id,
                UserRole.Technician,
                new ResolveTicketRequest
                {
                    ResolutionDescription =
                        "Sorun çözüldü."
                }));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CloseAsync_ByTicketCreator_ClosesAndSavesTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
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

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            technician.Id);

        ticket.Resolve(
            "Sorun giderildi.",
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = creator
                },
                NoOpAuditLogService);

        var result =
            await service.CloseAsync(
                ticket.Id,
                creator.Id,
                UserRole.Employee);

        Assert.Equal(
            TicketStatus.Closed,
            ticket.Status);

        Assert.Equal(
            "Closed",
            result.Status);

        Assert.NotNull(
            result.ClosedAt);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CloseAsync_ByAdmin_ClosesTicket()
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

        ticket.StartProgress(
            technician.Id);

        ticket.Resolve(
            "Sorun giderildi.",
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = admin
                },
                NoOpAuditLogService);

        await service.CloseAsync(
            ticket.Id,
            admin.Id,
            UserRole.Admin);

        Assert.Equal(
            TicketStatus.Closed,
            ticket.Status);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ReopenAsync_ByTicketCreator_ReopensAndSavesTicket()
    {
        var creator = CreateUser();
        var technician = CreateTechnician();
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

        ticket.Assign(
            technician.Id,
            Guid.NewGuid());

        ticket.StartProgress(
            technician.Id);

        ticket.Resolve(
            "Sorun giderildi.",
            technician.Id);

        ticket.Close(
            creator.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = creator
                },
                NoOpAuditLogService);

        var result =
            await service.ReopenAsync(
                ticket.Id,
                creator.Id,
                UserRole.Employee,
                new ReopenTicketRequest
                {
                    Reason = "Sorun yeniden oluştu."
                });

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Equal(
            "InProgress",
            result.Status);

        Assert.Null(result.ResolutionDescription);
        Assert.Null(result.ResolvedAt);
        Assert.Null(result.ClosedAt);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ReopenAsync_ByAdmin_ReopensTicket()
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

        ticket.StartProgress(
            technician.Id);

        ticket.Resolve(
            "Sorun giderildi.",
            technician.Id);

        ticket.Close(
            admin.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = admin
                },
                NoOpAuditLogService);

        await service.ReopenAsync(
            ticket.Id,
            admin.Id,
            UserRole.Admin,
            new ReopenTicketRequest
            {
                Reason = "Çözüm yeterli olmadı."
            });

        Assert.Equal(
            TicketStatus.InProgress,
            ticket.Status);

        Assert.Equal(
            1,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ReopenAsync_ByDifferentEmployee_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var differentEmployee = CreateUser();
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

        ticket.StartProgress(
            technician.Id);

        ticket.Resolve(
            "Sorun giderildi.",
            technician.Id);

        ticket.Close(
            creator.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = differentEmployee
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ReopenAsync(
                ticket.Id,
                differentEmployee.Id,
                UserRole.Employee,
                new ReopenTicketRequest
                {
                    Reason = "Sorun devam ediyor."
                }));

        Assert.Equal(
            TicketStatus.Closed,
            ticket.Status);

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ReopenAsync_ByTechnician_ThrowsForbiddenException()
    {
        var service =
new TicketCompletionService(
                new FakeTicketRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.ReopenAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Technician,
                new ReopenTicketRequest
                {
                    Reason = "Sorun devam ediyor."
                }));
    }

    [Fact]
    public async Task CloseAsync_ByDifferentEmployee_ThrowsForbiddenException()
    {
        var creator = CreateUser();
        var differentEmployee = CreateUser();
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

        ticket.StartProgress(
            technician.Id);

        ticket.Resolve(
            "Sorun giderildi.",
            technician.Id);

        var ticketRepository =
            new FakeTicketRepository
            {
                TicketById = ticket
            };

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository
                {
                    UserById = differentEmployee
                },
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CloseAsync(
                ticket.Id,
                differentEmployee.Id,
                UserRole.Employee));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CloseAsync_ByTechnician_ThrowsForbiddenException()
    {
        var ticketRepository =
            new FakeTicketRepository();

        var service =
new TicketCompletionService(
                ticketRepository,
                new FakeUserRepository(),
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.CloseAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                UserRole.Technician));

        Assert.Equal(
            0,
            ticketRepository.SaveChangesCallCount);
    }

}
