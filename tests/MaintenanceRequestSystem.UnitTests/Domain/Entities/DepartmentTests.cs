using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;

namespace MaintenanceRequestSystem.UnitTests.Domain.Entities;

public sealed class DepartmentTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesActiveDepartment()
    {
        // Arrange & Act
        var department = new Department(
            " Bilgi İşlem ",
            " Teknik destek ve sistem yönetimi ");

        // Assert
        Assert.NotEqual(Guid.Empty, department.Id);
        Assert.Equal("Bilgi İşlem", department.Name);
        Assert.Equal(
            "Teknik destek ve sistem yönetimi",
            department.Description);

        Assert.True(department.IsActive);
        Assert.NotEqual(default, department.CreatedAt);
        Assert.Null(department.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        // Act
        var action = () => new Department("   ");

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void UpdateDetails_WithValidValues_UpdatesDepartment()
    {
        // Arrange
        var department = new Department(
            "Bilgi İşlem",
            "Eski açıklama");

        // Act
        department.UpdateDetails(
            " Bilgi Teknolojileri ",
            " Yeni açıklama ");

        // Assert
        Assert.Equal("Bilgi Teknolojileri", department.Name);
        Assert.Equal("Yeni açıklama", department.Description);
        Assert.NotNull(department.UpdatedAt);
    }

    [Fact]
    public void Deactivate_WhenDepartmentIsActive_SetsInactive()
    {
        // Arrange
        var department = new Department("Bilgi İşlem");

        // Act
        department.Deactivate();

        // Assert
        Assert.False(department.IsActive);
        Assert.NotNull(department.UpdatedAt);
    }

    [Fact]
    public void Activate_WhenDepartmentIsInactive_SetsActive()
    {
        // Arrange
        var department = new Department("Bilgi İşlem");
        department.Deactivate();

        // Act
        department.Activate();

        // Assert
        Assert.True(department.IsActive);
        Assert.NotNull(department.UpdatedAt);
    }
}