using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Authentication.Dtos;
using MaintenanceRequestSystem.Application.Departments.Dtos;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.IntegrationTests.Infrastructure;

namespace MaintenanceRequestSystem.IntegrationTests.Assets;

public sealed class AssetManagementIntegrationTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AssetManagementIntegrationTests(
        CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetAssets_WithoutToken_ReturnsUnauthorized()
    {
        var response =
            await _client.GetAsync("/api/assets");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task GetAssets_WithEmployeeToken_ReturnsOk()
    {
        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/assets",
                employeeToken);

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAsset_WithEmployeeToken_ReturnsForbidden()
    {
        var employeeToken =
            await LoginAsync(
                CustomWebApplicationFactory.EmployeeEmail,
                CustomWebApplicationFactory.EmployeePassword);

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                employeeToken,
                new CreateAssetRequest());

        var response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode);
    }

    [Fact]
    public async Task CreateAsset_WithAdminToken_ReturnsCreated()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var serialNumber =
            $"asset-{Guid.NewGuid():N}";

        var createRequest =
            new CreateAssetRequest
            {
                Name = "Integration Test Bilgisayarı",
                SerialNumber = serialNumber,
                Type = AssetType.Computer,
                DepartmentId = departmentId,
                Location = "Test Odası"
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                createRequest);

        var response =
            await _client.SendAsync(request);

        var createdAsset =
            await response.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.Equal(
            HttpStatusCode.Created,
            response.StatusCode);

        Assert.NotNull(createdAsset);

        Assert.Equal(
            serialNumber.ToUpperInvariant(),
            createdAsset.SerialNumber);

        Assert.Equal(
            AssetType.Computer.ToString(),
            createdAsset.Type);

        Assert.True(createdAsset.IsActive);
    }

    [Fact]
    public async Task CreateAsset_WithDuplicateSerialNumber_ReturnsConflict()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var serialNumber =
            $"duplicate-{Guid.NewGuid():N}";

        var firstAsset =
            new CreateAssetRequest
            {
                Name = "Birinci Cihaz",
                SerialNumber = serialNumber,
                Type = AssetType.Computer,
                DepartmentId = departmentId
            };

        using var firstRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                firstAsset);

        var firstResponse =
            await _client.SendAsync(firstRequest);

        Assert.Equal(
            HttpStatusCode.Created,
            firstResponse.StatusCode);

        var secondAsset =
            new CreateAssetRequest
            {
                Name = "İkinci Cihaz",
                SerialNumber =
                    $"  {serialNumber.ToUpperInvariant()}  ",
                Type = AssetType.Server,
                DepartmentId = departmentId
            };

        using var secondRequest =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                secondAsset);

        var secondResponse =
            await _client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateAsset_WithAdminToken_ReturnsUpdatedAsset()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var createdAsset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        var updateRequest =
            new UpdateAssetRequest
            {
                Name = "Güncellenmiş Test Cihazı",
                SerialNumber = createdAsset.SerialNumber,
                Type = AssetType.Server,
                DepartmentId = departmentId,
                Location = "Sunucu Odası"
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Put,
                $"/api/assets/{createdAsset.Id}",
                adminToken,
                updateRequest);

        var response =
            await _client.SendAsync(request);

        var updatedAsset =
            await response.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.NotNull(updatedAsset);
        Assert.Equal("Güncellenmiş Test Cihazı", updatedAsset.Name);
        Assert.Equal("Server", updatedAsset.Type);
        Assert.Equal("Sunucu Odası", updatedAsset.Location);
        Assert.NotNull(updatedAsset.UpdatedAt);
    }

    [Fact]
    public async Task ChangeAssetStatus_WithAdminToken_DeactivatesAsset()
    {
        var adminToken =
            await LoginAsync(
                CustomWebApplicationFactory.AdminEmail,
                CustomWebApplicationFactory.AdminPassword);

        var departmentId =
            await GetActiveDepartmentIdAsync(adminToken);

        var createdAsset =
            await CreateAssetAsync(
                adminToken,
                departmentId);

        using var statusRequest =
            CreateAuthorizedRequest(
                HttpMethod.Patch,
                $"/api/assets/{createdAsset.Id}/status",
                adminToken,
                new ChangeAssetStatusRequest
                {
                    IsActive = false
                });

        var statusResponse =
            await _client.SendAsync(statusRequest);

        Assert.Equal(
            HttpStatusCode.NoContent,
            statusResponse.StatusCode);

        using var getRequest =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                $"/api/assets/{createdAsset.Id}",
                adminToken);

        var getResponse =
            await _client.SendAsync(getRequest);

        var asset =
            await getResponse.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.Equal(
            HttpStatusCode.OK,
            getResponse.StatusCode);

        Assert.NotNull(asset);
        Assert.False(asset.IsActive);
        Assert.NotNull(asset.UpdatedAt);
    }

    private async Task<AssetDto> CreateAssetAsync(
        string adminToken,
        Guid departmentId)
    {
        var requestBody =
            new CreateAssetRequest
            {
                Name = "Otomatik Test Cihazı",
                SerialNumber =
                    $"test-{Guid.NewGuid():N}",
                Type = AssetType.Computer,
                DepartmentId = departmentId,
                Location = "Test Odası"
            };

        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Post,
                "/api/assets",
                adminToken,
                requestBody);

        var response =
            await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var asset =
            await response.Content
                .ReadFromJsonAsync<AssetDto>();

        Assert.NotNull(asset);

        return asset;
    }

    private async Task<Guid> GetActiveDepartmentIdAsync(
        string accessToken)
    {
        using var request =
            CreateAuthorizedRequest(
                HttpMethod.Get,
                "/api/departments",
                accessToken);

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
}