using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Services;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.Common.Exceptions;

namespace MaintenanceRequestSystem.UnitTests.Application.AuditLogs;

public sealed class AuditLogServiceTests
{
    [Fact]
    public async Task AddAsync_WithValues_AddsSerializedAuditLog()
    {
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var performedByUserId =
            Guid.NewGuid();

        var ticketId =
            Guid.NewGuid();

        await service.AddAsync(
            performedByUserId,
            "TicketPriorityChanged",
            "Ticket",
            ticketId.ToString(),
            new
            {
                Priority = TicketPriority.Medium
            },
            new
            {
                Priority = TicketPriority.Critical
            });

        Assert.NotNull(
            repository.AddedAuditLog);

        Assert.Equal(
            performedByUserId,
            repository.AddedAuditLog.PerformedByUserId);

        Assert.Equal(
            "TicketPriorityChanged",
            repository.AddedAuditLog.Action);

        Assert.Equal(
            ticketId.ToString(),
            repository.AddedAuditLog.EntityId);

        Assert.Equal(
            "{\"priority\":\"Medium\"}",
            repository.AddedAuditLog.OldValues);

        Assert.Equal(
            "{\"priority\":\"Critical\"}",
            repository.AddedAuditLog.NewValues);

        Assert.Equal(
            1,
            repository.AddCallCount);
    }

    [Fact]
    public async Task GetPagedAsync_WhenPageNumberIsInvalid_ThrowsRequestValidationException()
    {
        // Arrange
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var query =
            new AuditLogListQuery
            {
                PageNumber = 0,
                PageSize = 10
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.GetPagedAsync(query));

        // Assert
        Assert.Equal(
            "Sayfa numarası en az 1 olmalıdır.",
            exception.Message);
    }
    [Fact]
    public async Task GetPagedAsync_WhenPageSizeIsInvalid_ThrowsRequestValidationException()
    {
        // Arrange
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var query =
            new AuditLogListQuery
            {
                PageNumber = 1,
                PageSize = 101
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.GetPagedAsync(query));

        // Assert
        Assert.Equal(
            "Sayfa boyutu 1 ile 100 arasında olmalıdır.",
            exception.Message);
    }

    [Fact]
    public async Task GetPagedAsync_WhenDateRangeIsInvalid_ThrowsRequestValidationException()
    {
        // Arrange
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var endDate =
            DateTime.UtcNow;

        var query =
            new AuditLogListQuery
            {
                StartDate =
                    endDate.AddDays(1),
                EndDate = endDate
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.GetPagedAsync(query));

        // Assert
        Assert.Equal(
            "Başlangıç tarihi bitiş tarihinden sonra olamaz.",
            exception.Message);
    }

    [Fact]
    public async Task GetPagedAsync_WhenPerformedByUserIdIsEmpty_ThrowsRequestValidationException()
    {
        // Arrange
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var query =
            new AuditLogListQuery
            {
                PerformedByUserId =
                    Guid.Empty
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.GetPagedAsync(query));

        // Assert
        Assert.Equal(
            "İşlemi yapan kullanıcı kimliği boş olamaz.",
            exception.Message);
    }

    [Fact]
    public async Task GetPagedAsync_WhenStartDateIsNotUtc_ThrowsRequestValidationException()
    {
        // Arrange
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var query =
            new AuditLogListQuery
            {
                StartDate =
                    DateTime.SpecifyKind(
                        DateTime.UtcNow,
                        DateTimeKind.Unspecified)
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.GetPagedAsync(query));

        // Assert
        Assert.Equal(
            "Başlangıç tarihi UTC formatında olmalıdır.",
            exception.Message);
    }

    [Fact]
    public async Task GetPagedAsync_WhenEndDateIsNotUtc_ThrowsRequestValidationException()
    {
        // Arrange
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        var query =
            new AuditLogListQuery
            {
                EndDate =
                    DateTime.SpecifyKind(
                        DateTime.UtcNow,
                        DateTimeKind.Unspecified)
            };

        // Act
        var exception =
            await Assert.ThrowsAsync<RequestValidationException>(
                () => service.GetPagedAsync(query));

        // Assert
        Assert.Equal(
            "Bitiş tarihi UTC formatında olmalıdır.",
            exception.Message);
    }

    [Fact]
    public async Task AddAsync_WithoutValues_AddsNullJsonValues()
    {
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        await service.AddAsync(
            Guid.NewGuid(),
            "TicketDeleted",
            "Ticket",
            Guid.NewGuid().ToString());

        Assert.NotNull(
            repository.AddedAuditLog);

        Assert.Null(
            repository.AddedAuditLog.OldValues);

        Assert.Null(
            repository.AddedAuditLog.NewValues);
    }
    [Fact]
    public async Task GetPagedAsync_WithValidQuery_ReturnsPagedAuditLogs()
    {
        // Arrange
        var performedByUser =
            new User(
                "Test Yöneticisi",
                $"audit-admin-{Guid.NewGuid():N}@example.com",
                "test-password-hash",
                UserRole.Admin,
                Guid.NewGuid());

        var entityId =
            Guid.NewGuid().ToString();

        var auditLog =
            new AuditLog(
                performedByUser.Id,
                "TicketAssigned",
                "Ticket",
                entityId,
                "{\"status\":\"Open\"}",
                "{\"status\":\"Assigned\"}");

        SetPerformedByUser(
            auditLog,
            performedByUser);

        var repository =
            new FakeAuditLogRepository
            {
                PagedItems =
                    new[]
                    {
                    auditLog
                    },
                TotalCount = 11
            };

        var service =
            new AuditLogService(repository);

        var query =
            new AuditLogListQuery
            {
                PageNumber = 2,
                PageSize = 10,
                PerformedByUserId =
                    performedByUser.Id,
                Action = "TicketAssigned",
                EntityName = "Ticket",
                EntityId = entityId,
                StartDate =
                    auditLog.CreatedAt.AddMinutes(-1),
                EndDate =
                    auditLog.CreatedAt.AddMinutes(1)
            };

        // Act
        var result =
            await service.GetPagedAsync(query);

        // Assert
        Assert.Equal(
            2,
            result.PageNumber);

        Assert.Equal(
            10,
            result.PageSize);

        Assert.Equal(
            11,
            result.TotalCount);

        Assert.Equal(
            2,
            result.TotalPages);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            auditLog.Id,
            item.Id);

        Assert.Equal(
            performedByUser.Id,
            item.PerformedByUserId);

        Assert.Equal(
            performedByUser.FullName,
            item.PerformedByUserFullName);

        Assert.Equal(
            "TicketAssigned",
            item.Action);

        Assert.Equal(
            "Ticket",
            item.EntityName);

        Assert.Equal(
            entityId,
            item.EntityId);

        Assert.Equal(
            "{\"status\":\"Open\"}",
            item.OldValues);

        Assert.Equal(
            "{\"status\":\"Assigned\"}",
            item.NewValues);

        Assert.Equal(
            auditLog.CreatedAt,
            item.CreatedAt);
    }

    [Fact]
    public async Task AddAsync_WithEmptyUserId_DoesNotCallRepository()
    {
        var repository =
            new FakeAuditLogRepository();

        var service =
            new AuditLogService(repository);

        await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddAsync(
                Guid.Empty,
                "TicketDeleted",
                "Ticket",
                Guid.NewGuid().ToString()));

        Assert.Equal(
            0,
            repository.AddCallCount);
    }

    /// <summary>
    /// Audit kaydının işlemi yapan kullanıcı navigation alanını
    /// test amacıyla ayarlar.
    /// </summary>
    private static void SetPerformedByUser(
        AuditLog auditLog,
        User performedByUser)
    {
        typeof(AuditLog)
            .GetProperty(
                nameof(AuditLog.PerformedByUser))!
            .SetValue(
                auditLog,
                performedByUser);
    }

    private sealed class FakeAuditLogRepository
        : IAuditLogRepository
    {

        public IReadOnlyList<AuditLog> PagedItems { get; set; }
    = Array.Empty<AuditLog>();

        public int TotalCount { get; set; }
        public AuditLog? AddedAuditLog { get; private set; }

        public int AddCallCount { get; private set; }

        public Task AddAsync(
            AuditLog auditLog,
            CancellationToken cancellationToken = default)
        {
            AddedAuditLog = auditLog;
            AddCallCount++;

            return Task.CompletedTask;
        }

        public Task<(
    IReadOnlyList<AuditLog> Items,
    int TotalCount)> GetPagedAsync(
    AuditLogListQuery query,
    CancellationToken cancellationToken = default)
        {
            return Task.FromResult((
                PagedItems,
                TotalCount));
        }
        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
