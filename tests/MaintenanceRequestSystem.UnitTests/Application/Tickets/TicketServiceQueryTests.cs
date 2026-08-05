using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Tickets.Dtos;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Tickets;

public sealed partial class TicketServiceTests
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

}
