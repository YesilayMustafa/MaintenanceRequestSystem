using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Application.Users.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class AuthenticationAuthorizationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationAuthorizationTests(
        CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }
    [Fact]
    public async Task ChangeDepartmentStatus_WithEmployeeToken_ReturnsForbidden()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        var department =
            await CreateDepartmentAsync(adminToken);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/departments/{department.Id}/status");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                employeeToken);

        request.Content =
            JsonContent.Create(
                new ChangeDepartmentStatusRequest
                {
                    IsActive = false
                });

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangeDepartmentStatus_WithAdminToken_ReturnsNoContent()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var department =
            await CreateDepartmentAsync(adminToken);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Patch,
                $"/api/departments/{department.Id}/status");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                adminToken);

        request.Content =
            JsonContent.Create(
                new ChangeDepartmentStatusRequest
                {
                    IsActive = false
                });

        var response = await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.NoContent,
            response.StatusCode);
    }

    [Fact]
    public async Task ChangeDepartmentStatus_WithTechnicianToken_ReturnsForbidden()
    {
        var adminToken = await LoginAsync(
            CustomWebApplicationFactory.AdminEmail,
            CustomWebApplicationFactory.AdminPassword);
        var department = await CreateDepartmentAsync(adminToken);
        var technician = await CreateTechnicianAsync(
            adminToken,
            department.Id);
        var technicianToken = await LoginAsync(
            technician.Email,
            technician.Password);
        using var request = new HttpRequestMessage(
            HttpMethod.Patch,
            $"/api/departments/{department.Id}/status");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            technicianToken);
        request.Content = JsonContent.Create(
            new ChangeDepartmentStatusRequest
            {
                IsActive = false
            });

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetDepartments_WithoutToken_ReturnsUnauthorized()
    {
        // Act
        var response =
            await _client.GetAsync("/api/departments");

        // Assert
        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task ApiResponse_IncludesLowRiskSecurityHeaders()
    {
        var response = await _client.GetAsync("/api/departments");

        Assert.Equal(
            "nosniff",
            response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(
            "no-referrer",
            response.Headers.GetValues("Referrer-Policy").Single());
    }

    [Fact]
    public async Task CorsPreflight_WithConfiguredOrigin_AllowsOrigin()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/departments");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.Equal(
            "http://localhost:5173",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
    }

    [Fact]
    public async Task CorsPreflight_WithUnconfiguredOrigin_DoesNotAllowOrigin()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Options,
            "/api/departments");
        request.Headers.Add("Origin", "https://untrusted.example");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public void JwtBearerOptions_UseValidatedProductionWiring()
    {
        using var scope = _factory.Services.CreateScope();
        var options = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var validation = options.TokenValidationParameters;

        Assert.True(validation.ValidateIssuerSigningKey);
        Assert.True(validation.ValidateIssuer);
        Assert.True(validation.ValidateAudience);
        Assert.True(validation.ValidateLifetime);
        Assert.True(validation.RequireExpirationTime);
        Assert.True(validation.RequireSignedTokens);
        Assert.NotNull(validation.IssuerSigningKey);
        Assert.True(validation.IssuerSigningKey.KeySize >= 256);
        Assert.Contains(
            SecurityAlgorithms.HmacSha256,
            validation.ValidAlgorithms);
        Assert.NotNull(options.Events.OnTokenValidated);
    }

    [Fact]
    public async Task Login_WithValidAdminCredentials_ReturnsToken()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email =
                CustomWebApplicationFactory.AdminEmail,

            Password =
                CustomWebApplicationFactory.AdminPassword
        };

        // Act
        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        // Assert
        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(result);

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.AccessToken));

        Assert.Equal(
            "Admin",
            result.User.Role);
    }

    [Fact]
    public async Task CreateDepartment_WithEmployeeToken_ReturnsForbidden()
    {
        // Arrange
        var token =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/departments");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        request.Content =
            JsonContent.Create(
                new CreateDepartmentRequest
                {
                    Name =
                        $"Employee Yetki Testi {Guid.NewGuid()}",

                    Description =
                        "Employee rolüyle kayıt denemesi"
                });

        // Act
        var response =
            await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateDepartment_WithAdminToken_ReturnsCreated()
    {
        // Arrange
        var token =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/departments");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token);

        request.Content =
            JsonContent.Create(
                new CreateDepartmentRequest
                {
                    Name =
                        $"Admin Yetki Testi {Guid.NewGuid()}",

                    Description =
                        "Admin rolüyle oluşturulan departman"
                });

        // Act
        var response =
            await _client.SendAsync(request);

        // Assert
        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);
    }

    private async Task<string> LoginAsync(
        string email,
        string password)
    {
        var request = new LoginRequest
        {
            Email = email,
            Password = password
        };

        var response =
            await _client.PostAsJsonAsync(
                "/api/auth/login",
                request);

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content
                .ReadFromJsonAsync<LoginResponse>();

        Assert.NotNull(result);

        return result.AccessToken;
    }

    private async Task<DepartmentDto> CreateDepartmentAsync(
    string adminToken)
    {
        using var request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "/api/departments");

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                adminToken);

        request.Content =
            JsonContent.Create(
                new CreateDepartmentRequest
                {
                    Name =
                        $"Yetki Test Departmanı {Guid.NewGuid()}",

                    Description =
                        "Integration test için oluşturuldu."
                });

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var department =
            await response.Content
                .ReadFromJsonAsync<DepartmentDto>();

        Assert.NotNull(department);

        return department;
    }

    private async Task<CreatedTechnician> CreateTechnicianAsync(
        string adminToken,
        Guid departmentId)
    {
        var email = $"security-tech-{Guid.NewGuid():N}@example.com";
        const string password = "SecurityTech123!";
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/users");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            adminToken);
        request.Content = JsonContent.Create(
            new CreateUserRequest
            {
                FullName = "Security Teknik Personeli",
                Email = email,
                Password = password,
                Role = UserRole.Technician,
                DepartmentId = departmentId
            });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        return new CreatedTechnician(email, password);
    }

    private sealed record CreatedTechnician(
        string Email,
        string Password);
}
