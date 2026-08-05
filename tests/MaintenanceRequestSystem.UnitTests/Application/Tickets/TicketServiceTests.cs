using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Services;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed class TicketServiceTests
{
    private static readonly IAuditLogService NoOpAuditLogService =
        new NullAuditLogService();

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
                },
                NoOpAuditLogService);

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
                new FakeUserRepository(),
                NoOpAuditLogService);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetByIdAsync(
                Guid.NewGuid(),
                Guid.NewGuid(),
                (UserRole)999));
    }

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = differentTechnician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = differentTechnician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = technician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
                new FakeUserRepository
                {
                    UserById = differentTechnician
                },
                NoOpAuditLogService);

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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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
new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
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
            new TicketService(
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
new TicketService(
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
new TicketService(
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
new TicketService(
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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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
new TicketService(
                ticketRepository,
                new FakeAssetRepository(),
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

    [Fact]
    public async Task GetPagedAsync_WithUndefinedStatus_ThrowsValidationException()
    {
        var service =
new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
        var auditLogService =
            new FakeAuditLogService();

        var service =
            new TicketService(
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
new TicketService(
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
new TicketService(
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
    public async Task GetPagedAsync_WithOverflowingOffset_ThrowsValidationException()
    {
        var service =
new TicketService(
                new FakeTicketRepository(),
                new FakeAssetRepository(),
                new FakeUserRepository(),
                NoOpAuditLogService);

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
                new FakeUserRepository(),
                NoOpAuditLogService);

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
new TicketService(
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
new TicketService(
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
new TicketService(
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
            new TicketService(
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
new TicketService(
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
                },
                NoOpAuditLogService);

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
                },
                NoOpAuditLogService);

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
                },
                NoOpAuditLogService);

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
                },
                NoOpAuditLogService);

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
                new FakeUserRepository(),
                NoOpAuditLogService);

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
                new FakeUserRepository(),
                NoOpAuditLogService);

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
