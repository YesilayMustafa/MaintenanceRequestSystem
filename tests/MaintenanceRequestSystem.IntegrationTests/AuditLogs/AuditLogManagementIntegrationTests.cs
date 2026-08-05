using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.AuditLogs.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Common.Models;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MaintenanceRequestSystem.IntegrationTests.AuditLogs;

public sealed class AuditLogManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuditLogManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAuditLogs_ByAdmin_ReturnsPagedAuditLogs()
    {
        var entityId =
            Guid.NewGuid().ToString();

        var seededAuditLog =
            await SeedAuditLogAsync(
                $"PagedAudit-{Guid.NewGuid():N}",
                "Ticket",
                entityId);

        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/audit-logs" +
                $"?entityId={Uri.EscapeDataString(entityId)}" +
                "&pageNumber=1" +
                "&pageSize=10",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<AuditLogDto>>();

        Assert.NotNull(result);

        Assert.Equal(
            1,
            result.PageNumber);

        Assert.Equal(
            10,
            result.PageSize);

        Assert.Equal(
            1,
            result.TotalCount);

        Assert.Equal(
            1,
            result.TotalPages);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            seededAuditLog.Id,
            item.Id);

        Assert.Equal(
            seededAuditLog.PerformedByUserId,
            item.PerformedByUserId);

        Assert.Equal(
            seededAuditLog.PerformedByUserFullName,
            item.PerformedByUserFullName);

        Assert.Equal(
            seededAuditLog.Action,
            item.Action);

        Assert.Equal(
            seededAuditLog.EntityName,
            item.EntityName);

        Assert.Equal(
            seededAuditLog.EntityId,
            item.EntityId);

        Assert.Equal(
            seededAuditLog.OldValues,
            item.OldValues);

        Assert.Equal(
            seededAuditLog.NewValues,
            item.NewValues);
    }

    [Fact]
    public async Task GetAuditLogs_ByEmployee_ReturnsForbidden()
    {
        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/audit-logs",
                employeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithActionFilter_ReturnsMatchingRecords()
    {
        var matchingAction =
            $"MatchingAction-{Guid.NewGuid():N}";

        var differentAction =
            $"DifferentAction-{Guid.NewGuid():N}";

        var matchingAuditLog =
            await SeedAuditLogAsync(
                matchingAction,
                "Ticket",
                Guid.NewGuid().ToString());

        await SeedAuditLogAsync(
            differentAction,
            "Ticket",
            Guid.NewGuid().ToString());

        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/audit-logs" +
                $"?action={Uri.EscapeDataString(matchingAction)}",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        var result =
            await response.Content
                .ReadFromJsonAsync<PagedResult<AuditLogDto>>();

        Assert.NotNull(result);

        Assert.Equal(
            1,
            result.TotalCount);

        var item =
            Assert.Single(result.Items);

        Assert.Equal(
            matchingAuditLog.Id,
            item.Id);

        Assert.Equal(
            matchingAction,
            item.Action);
    }

    [Fact]
    public async Task GetAuditLogs_WithNonUtcStartDate_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        const string nonUtcStartDate =
            "2026-08-05T12:00:00";

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/audit-logs" +
                $"?startDate={Uri.EscapeDataString(nonUtcStartDate)}",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithInvalidPageSize_ReturnsBadRequest()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/audit-logs?pageSize=101",
                adminToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    private async Task<SeededAuditLog> SeedAuditLogAsync(
        string action,
        string entityName,
        string entityId)
    {
        using var scope =
            _factory.Services.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var performedByUser =
            await dbContext.Users
                .SingleAsync(
                    user =>
                        user.Email ==
                        CustomWebApplicationFactory.AdminEmail);

        const string oldValues =
            "{\"status\":\"Open\"}";

        const string newValues =
            "{\"status\":\"Assigned\"}";

        var auditLog =
            new AuditLog(
                performedByUser.Id,
                action,
                entityName,
                entityId,
                oldValues,
                newValues);

        await dbContext.AuditLogs.AddAsync(
            auditLog);

        await dbContext.SaveChangesAsync();

        return new SeededAuditLog(
            auditLog.Id,
            performedByUser.Id,
            performedByUser.FullName,
            auditLog.Action,
            auditLog.EntityName,
            auditLog.EntityId,
            auditLog.OldValues,
            auditLog.NewValues);
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var loginRequest =
            new LoginRequest
            {
                Email = email,
                Password = password
            };

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);

        return result.AccessToken;
    }

    private static HttpRequestMessage CreateAuthorizedRequest(
        HttpMethod method,
        string requestUri,
        string accessToken,
        object? content = null)
    {
        var request =
            new HttpRequestMessage(
                method,
                requestUri);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        if (content is not null)
        {
            request.Content =
                JsonContent.Create(content);
        }

        return request;
    }

    private sealed record SeededAuditLog(
        Guid Id,
        Guid PerformedByUserId,
        string PerformedByUserFullName,
        string Action,
        string EntityName,
        string EntityId,
        string? OldValues,
        string? NewValues);
}
