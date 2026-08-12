using System;
using System.Collections.Generic;
using System.Text;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Authentication;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using System.IdentityModel.Tokens.Jwt;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.IntegrationTests.Users;

public sealed class UserManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_WithoutToken_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync("/api/users");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithEmployeeToken_ReturnsForbidden()
    {
        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/users",
                employeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithAdminToken_ReturnsCreated()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var email =
            $"created-{Guid.NewGuid():N}@example.com";

        var createRequest =
            new CreateUserRequest
            {
                FullName = "Integration Kullanıcısı",
                Email = email,
                Password = "UserTest123!",
                Role = UserRole.Employee,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                createRequest);

        var response =
            await _client.SendAsync(request);

        var responseJson =
            await response.Content.ReadAsStringAsync();

        var createdUser =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(createdUser);

        Assert.Equal(
            email,
            createdUser.Email);

        Assert.Equal(
            UserRole.Employee.ToString(),
            createdUser.Role);

        Assert.True(createdUser.IsActive);

        Assert.DoesNotContain(
            "password",
            responseJson.ToLowerInvariant());
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var email =
            $"duplicate-{Guid.NewGuid():N}@example.com";

        var firstRequest =
            new CreateUserRequest
            {
                FullName = "Birinci Kullanıcı",
                Email = email,
                Password = "UserTest123!",
                Role = UserRole.Employee,
                DepartmentId = departmentId
            };

        using var firstHttpRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                firstRequest);

        var firstResponse =
            await _client.SendAsync(firstHttpRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondRequest =
            new CreateUserRequest
            {
                FullName = "İkinci Kullanıcı",
                Email = email,
                Password = "UserTest456!",
                Role = UserRole.Technician,
                DepartmentId = departmentId
            };

        using var secondHttpRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                secondRequest);

        var secondResponse =
            await _client.SendAsync(secondHttpRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WithAdminToken_ReturnsUpdatedUser()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var createdUser =
            await CreateUserAsync(
                adminToken,
                departmentId);

        var updatedEmail =
            $"updated-{Guid.NewGuid():N}@example.com";

        var updateRequest =
            new UpdateUserRequest
            {
                FullName = "Güncellenmiş Kullanıcı",
                Email = updatedEmail,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"/api/users/{createdUser.Id}",
                adminToken,
                updateRequest);

        var response =
            await _client.SendAsync(request);

        var updatedUser =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(updatedUser);

        Assert.Equal(
            "Güncellenmiş Kullanıcı",
            updatedUser.FullName);

        Assert.Equal(
            updatedEmail,
            updatedUser.Email);

        Assert.NotNull(
            updatedUser.UpdatedAt);
    }

    [Fact]
    public async Task ChangeUserRole_WithAdminToken_ChangesLoginRole()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var password = "UserTest123!";

        var createdUser =
            await CreateUserAsync(
                adminToken,
                departmentId,
                password);

        var oldAccessToken =
            await LoginAsync(
                createdUser.Email,
                password);

        var roleRequest =
            new ChangeUserRoleRequest
            {
                Role = UserRole.Technician
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/users/{createdUser.Id}/role",
                adminToken,
                roleRequest);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);

        using var oldTokenRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/auth/me",
                oldAccessToken);

        var oldTokenResponse =
            await _client.SendAsync(oldTokenRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldTokenResponse.StatusCode);

        var loginResponse =
            await LoginForResponseAsync(
                createdUser.Email,
                password);

        Assert.Equal(
            UserRole.Technician.ToString(),
            loginResponse.User.Role);
    }

    [Fact]
    public async Task DeactivateUser_PreventsLogin()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var password = "UserTest123!";

        var createdUser =
            await CreateUserAsync(
                adminToken,
                departmentId,
                password);

        var oldAccessToken =
            await LoginAsync(
                createdUser.Email,
                password);

        var statusRequest =
            new ChangeUserStatusRequest
            {
                IsActive = false
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/users/{createdUser.Id}/status",
                adminToken,
                statusRequest);

        var statusResponse =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            statusResponse.StatusCode);

        using var oldTokenRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/auth/me",
                oldAccessToken);

        var oldTokenResponse =
            await _client.SendAsync(oldTokenRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            oldTokenResponse.StatusCode);

        var loginRequest =
            new LoginRequest
            {
                Email = createdUser.Email,
                Password = password
            };

        var loginResponse =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                loginRequest);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            loginResponse.StatusCode);

        using var scope = _factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        Assert.True(await context.AuditLogs.AnyAsync(
            auditLog =>
                auditLog.Action == "UserDeactivated" &&
                auditLog.EntityId == createdUser.Id.ToString()));
    }

    [Fact]
    public async Task Login_WithValidUser_IncludesSecurityVersionClaim()
    {
        // Arrange
        var loginResponse = await LoginForResponseAsync(
            CustomWebApplicationFactory.EmployeeEmail,
            CustomWebApplicationFactory.EmployeePassword);

        // Act
        var token = new JwtSecurityTokenHandler()
            .ReadJwtToken(loginResponse.AccessToken);

        // Assert
        Assert.Contains(
            token.Claims,
            claim =>
                claim.Type == AuthenticationClaimNames.SecurityVersion &&
                claim.Value == "1");
    }

    [Fact]
    public async Task GetCurrentUser_AfterProfileUpdate_ReturnsDatabaseValues()
    {
        // Arrange
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var password = "UserTest123!";
        var createdUser = await CreateUserAsync(
            adminToken,
            departmentId,
            password);

        var userToken = await LoginAsync(
            createdUser.Email,
            password);

        var updatedEmail =
            $"profile-{Guid.NewGuid():N}@example.com";

        using var updateRequest = CreateAuthorizedRequest(
            HttpMethod.Put,
            $"/api/users/{createdUser.Id}",
            adminToken,
            new UpdateUserRequest
            {
                FullName = "Veritabanı Profil Kullanıcısı",
                Email = updatedEmail,
                DepartmentId = departmentId
            });

        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.EnsureSuccessStatusCode();

        using var currentUserRequest = CreateAuthorizedRequest(
            HttpMethod.Get,
            "/api/auth/me",
            userToken);

        // Act
        var response = await _client.SendAsync(currentUserRequest);
        var currentUser = await response.Content
            .ReadFromJsonAsync<CurrentUserDto>();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(currentUser);
        Assert.Equal("Veritabanı Profil Kullanıcısı", currentUser.FullName);
        Assert.Equal(updatedEmail, currentUser.Email);
        Assert.Equal(departmentId, currentUser.DepartmentId);
        Assert.False(string.IsNullOrWhiteSpace(currentUser.DepartmentName));
        Assert.Equal("Active", currentUser.AccountStatus);
    }

    [Fact]
    public async Task DeactivateCurrentAdmin_ReturnsForbidden()
    {
        // Arrange
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var adminId = await context.Users
            .Where(user =>
                user.Email == CustomWebApplicationFactory.AdminEmail)
            .Select(user => user.Id)
            .SingleAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/users/{adminId}/status",
            adminToken,
            new ChangeUserStatusRequest
            {
                IsActive = false
            });

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DemoteLastActiveAdmin_ReturnsConflict()
    {
        // Arrange
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        var adminId = await context.Users
            .Where(user =>
                user.Email == CustomWebApplicationFactory.AdminEmail)
            .Select(user => user.Id)
            .SingleAsync();

        using var request = CreateAuthorizedRequest(
            HttpMethod.Patch,
            $"/api/users/{adminId}/role",
            adminToken,
            new ChangeUserRoleRequest
            {
                Role = UserRole.Employee
            });

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<UserDto> CreateUserAsync(
        string adminToken,
        Guid departmentId,
        string password = "UserTest123!")
    {
        var createRequest =
            new CreateUserRequest
            {
                FullName = "Otomatik Test Kullanıcısı",
                Email =
                    $"user-{Guid.NewGuid():N}@example.com",
                Password = password,
                Role = UserRole.Employee,
                DepartmentId = departmentId
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/users",
                adminToken,
                createRequest);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var createdUser =
            await response.Content
                .ReadFromJsonAsync<UserDto>();

        Assert.NotNull(createdUser);

        return createdUser;
    }

    private async Task<Guid> GetActiveDepartmentIdAsync(
        string adminToken)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/departments",
                adminToken);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var departments =
            await response.Content
                .ReadFromJsonAsync<List<DepartmentDto>>();

        Assert.NotNull(departments);

        var activeDepartment =
            departments.FirstOrDefault(
                department => department.IsActive);

        Assert.NotNull(activeDepartment);

        return activeDepartment.Id;
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var response =
            await LoginForResponseAsync(
                email,
                password);

        return response.AccessToken;
    }

    private async Task<LoginResponse> LoginForResponseAsync(
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

        return result;
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
}
