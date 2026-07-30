using MaintenanceRequestSystem.Application.Assets.Dtos;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Assets.Services;
using MaintenanceRequestSystem.Application.Common.Exceptions;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Application.Assets;

public sealed class AssetServiceTests
{
    [Fact]
    public async Task CreateAsync_WithValidRequest_AddsAndSavesAsset()
    {
        var department = CreateDepartment();

        var assetRepository = new FakeAssetRepository();

        var departmentRepository =
            new FakeDepartmentRepository
            {
                DepartmentById = department
            };

        var service =
            new AssetService(
                assetRepository,
                departmentRepository);

        var request =
            new CreateAssetRequest
            {
                Name = "  Dell Latitude 5540  ",
                SerialNumber = "  dl-5540-001  ",
                Type = AssetType.Computer,
                DepartmentId = department.Id,
                Location = "  Bilgi İşlem - Oda 204  "
            };

        var result =
            await service.CreateAsync(request);

        var createdAsset =
            Assert.Single(assetRepository.Assets);

        Assert.True(assetRepository.AddCalled);
        Assert.Equal(1, assetRepository.SaveChangesCallCount);
        Assert.Equal("Dell Latitude 5540", createdAsset.Name);
        Assert.Equal("DL-5540-001", createdAsset.SerialNumber);
        Assert.Equal("Bilgi İşlem - Oda 204", createdAsset.Location);
        Assert.Equal(department.Id, createdAsset.DepartmentId);
        Assert.Equal("Bilgi İşlem", result.DepartmentName);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateSerialNumber_ThrowsConflictException()
    {
        var assetRepository =
            new FakeAssetRepository
            {
                SerialNumberExistsResult = true
            };

        var service =
            new AssetService(
                assetRepository,
                new FakeDepartmentRepository
                {
                    DepartmentById = CreateDepartment()
                });

        var request =
            CreateAssetRequestFor(Guid.NewGuid());

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(request));

        Assert.False(assetRepository.AddCalled);
        Assert.Equal(0, assetRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithMissingDepartment_ThrowsKeyNotFoundException()
    {
        var assetRepository =
            new FakeAssetRepository();

        var service =
            new AssetService(
                assetRepository,
                new FakeDepartmentRepository
                {
                    DepartmentById = null
                });

        var request =
            CreateAssetRequestFor(Guid.NewGuid());

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateAsync(request));

        Assert.False(assetRepository.AddCalled);
        Assert.Equal(0, assetRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task CreateAsync_WithInactiveDepartment_ThrowsValidationException()
    {
        var inactiveDepartment =
            CreateDepartment(isActive: false);

        var assetRepository =
            new FakeAssetRepository();

        var service =
            new AssetService(
                assetRepository,
                new FakeDepartmentRepository
                {
                    DepartmentById = inactiveDepartment
                });

        var request =
            CreateAssetRequestFor(
                inactiveDepartment.Id);

        await Assert.ThrowsAsync<RequestValidationException>(
            () => service.CreateAsync(request));

        Assert.False(assetRepository.AddCalled);
        Assert.Equal(0, assetRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesAndSavesAsset()
    {
        var asset = CreateAsset();
        var department = CreateDepartment();

        var assetRepository =
            new FakeAssetRepository
            {
                AssetById = asset
            };

        var service =
            new AssetService(
                assetRepository,
                new FakeDepartmentRepository
                {
                    DepartmentById = department
                });

        var request =
            new UpdateAssetRequest
            {
                Name = "Güncel Cihaz",
                SerialNumber = "updated-001",
                Type = AssetType.Server,
                DepartmentId = department.Id,
                Location = "Sunucu Odası"
            };

        var result =
            await service.UpdateAsync(
                asset.Id,
                request);

        Assert.Equal("Güncel Cihaz", asset.Name);
        Assert.Equal("UPDATED-001", asset.SerialNumber);
        Assert.Equal(AssetType.Server, asset.Type);
        Assert.Equal(department.Id, asset.DepartmentId);
        Assert.Equal("Sunucu Odası", asset.Location);
        Assert.NotNull(asset.UpdatedAt);
        Assert.Equal(1, assetRepository.SaveChangesCallCount);
        Assert.Equal("Bilgi İşlem", result.DepartmentName);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateSerialNumber_ThrowsConflictException()
    {
        var asset = CreateAsset();

        var assetRepository =
            new FakeAssetRepository
            {
                AssetById = asset,
                SerialNumberExistsResult = true
            };

        var service =
            new AssetService(
                assetRepository,
                new FakeDepartmentRepository
                {
                    DepartmentById = CreateDepartment()
                });

        var request =
            new UpdateAssetRequest
            {
                Name = "Güncel Cihaz",
                SerialNumber = "DUPLICATE-001",
                Type = AssetType.Server,
                DepartmentId = Guid.NewGuid(),
                Location = "Yeni Konum"
            };

        await Assert.ThrowsAsync<ConflictException>(
            () => service.UpdateAsync(
                asset.Id,
                request));

        Assert.Equal(
            "Dell Latitude 5540",
            asset.Name);

        Assert.Equal(
            "DL-5540-001",
            asset.SerialNumber);

        Assert.Equal(
            0,
            assetRepository.SaveChangesCallCount);
    }

    [Fact]
    public async Task ChangeStatusAsync_WithInactiveRequest_DeactivatesAsset()
    {
        var asset = CreateAsset();

        var assetRepository =
            new FakeAssetRepository
            {
                AssetById = asset
            };

        var service =
            new AssetService(
                assetRepository,
                new FakeDepartmentRepository());

        await service.ChangeStatusAsync(
            asset.Id,
            new ChangeAssetStatusRequest
            {
                IsActive = false
            });

        Assert.False(asset.IsActive);
        Assert.NotNull(asset.UpdatedAt);
        Assert.Equal(1, assetRepository.SaveChangesCallCount);
    }

    private static CreateAssetRequest CreateAssetRequestFor(
        Guid departmentId)
    {
        return new CreateAssetRequest
        {
            Name = "Dell Latitude 5540",
            SerialNumber = "DL-5540-001",
            Type = AssetType.Computer,
            DepartmentId = departmentId,
            Location = "Bilgi İşlem"
        };
    }

    private static Asset CreateAsset()
    {
        return new Asset(
            "Dell Latitude 5540",
            "DL-5540-001",
            AssetType.Computer,
            Guid.NewGuid(),
            "Bilgi İşlem");
    }

    private static Department CreateDepartment(
        bool isActive = true)
    {
        var department =
            new Department(
                "Bilgi İşlem",
                "Teknik destek ve sistem yönetimi");

        if (!isActive)
        {
            department.Deactivate();
        }

        return department;
    }

    private sealed class FakeAssetRepository
        : IAssetRepository
    {
        public List<Asset> Assets { get; } = [];

        public Asset? AssetById { get; init; }

        public bool SerialNumberExistsResult { get; init; }

        public bool AddCalled { get; private set; }

        public int SaveChangesCallCount { get; private set; }

        public Task<IReadOnlyList<Asset>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Asset>>(
                Assets);
        }

        public Task<Asset?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            var asset =
                AssetById ??
                Assets.FirstOrDefault(
                    existingAsset =>
                        existingAsset.Id == id);

            return Task.FromResult(asset);
        }

        public Task<bool> SerialNumberExistsAsync(
            string serialNumber,
            Guid? excludedAssetId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                SerialNumberExistsResult);
        }

        public Task AddAsync(
            Asset asset,
            CancellationToken cancellationToken = default)
        {
            AddCalled = true;
            Assets.Add(asset);

            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;

            return Task.CompletedTask;
        }
    }

    private sealed class FakeDepartmentRepository
        : IDepartmentRepository
    {
        public Department? DepartmentById { get; init; }

        public Task<IReadOnlyList<Department>> GetAllAsync(
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Department> departments =
                DepartmentById is null
                    ? []
                    : [DepartmentById];

            return Task.FromResult(departments);
        }

        public Task<Department?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                DepartmentById);
        }

        public Task<bool> ExistsByNameAsync(
            string name,
            Guid? excludedDepartmentId = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AddAsync(
            Department department,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }
}