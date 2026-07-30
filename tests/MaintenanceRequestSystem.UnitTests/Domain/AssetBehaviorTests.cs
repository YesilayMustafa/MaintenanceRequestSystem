using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed class AssetBehaviorTests
{
    [Fact]
    public void Constructor_WithValidValues_NormalizesAndCreatesAsset()
    {
        var departmentId = Guid.NewGuid();

        var asset = new Asset(
            "  Dell Latitude 5540  ",
            "  dl-5540-001  ",
            AssetType.Computer,
            departmentId,
            "  Bilgi İşlem - Oda 204  ");

        Assert.NotEqual(Guid.Empty, asset.Id);
        Assert.Equal("Dell Latitude 5540", asset.Name);
        Assert.Equal("DL-5540-001", asset.SerialNumber);
        Assert.Equal(AssetType.Computer, asset.Type);
        Assert.Equal(departmentId, asset.DepartmentId);
        Assert.Equal("Bilgi İşlem - Oda 204", asset.Location);
        Assert.True(asset.IsActive);
        Assert.Null(asset.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithInvalidType_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new Asset(
                "Test Cihazı",
                "TEST-001",
                (AssetType)999,
                Guid.NewGuid()));
    }

    [Fact]
    public void UpdateDetails_WithValidValues_UpdatesAsset()
    {
        var asset = CreateAsset();
        var newDepartmentId = Guid.NewGuid();

        var beforeUpdate = DateTime.UtcNow;

        asset.UpdateDetails(
            "  Güncel Bilgisayar  ",
            "  updated-001  ",
            AssetType.Server,
            newDepartmentId,
            "  Sunucu Odası  ");

        Assert.Equal("Güncel Bilgisayar", asset.Name);
        Assert.Equal("UPDATED-001", asset.SerialNumber);
        Assert.Equal(AssetType.Server, asset.Type);
        Assert.Equal(newDepartmentId, asset.DepartmentId);
        Assert.Equal("Sunucu Odası", asset.Location);
        Assert.NotNull(asset.UpdatedAt);

        Assert.InRange(
            asset.UpdatedAt.Value,
            beforeUpdate,
            DateTime.UtcNow);
    }

    [Fact]
    public void UpdateDetails_WithInvalidSerialNumber_DoesNotPartiallyUpdateAsset()
    {
        var asset = CreateAsset();

        var originalName = asset.Name;
        var originalSerialNumber = asset.SerialNumber;
        var originalType = asset.Type;
        var originalDepartmentId = asset.DepartmentId;
        var originalLocation = asset.Location;

        Assert.Throws<ArgumentException>(
            () => asset.UpdateDetails(
                "Değişmemesi Gereken Ad",
                " ",
                AssetType.Server,
                Guid.NewGuid(),
                "Yeni Konum"));

        Assert.Equal(originalName, asset.Name);
        Assert.Equal(originalSerialNumber, asset.SerialNumber);
        Assert.Equal(originalType, asset.Type);
        Assert.Equal(originalDepartmentId, asset.DepartmentId);
        Assert.Equal(originalLocation, asset.Location);
        Assert.Null(asset.UpdatedAt);
    }

    [Fact]
    public void Deactivate_SetsAssetAsInactive()
    {
        var asset = CreateAsset();

        asset.Deactivate();

        Assert.False(asset.IsActive);
        Assert.NotNull(asset.UpdatedAt);
    }

    [Fact]
    public void Activate_AfterDeactivation_SetsAssetAsActive()
    {
        var asset = CreateAsset();

        asset.Deactivate();
        asset.Activate();

        Assert.True(asset.IsActive);
        Assert.NotNull(asset.UpdatedAt);
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
}