using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;

namespace MaintenanceRequestSystem.IntegrationTests.Authentication;

public sealed class AuthenticationAuthorizationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthenticationAuthorizationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
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
}