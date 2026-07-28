using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain.Entities;

public sealed class UserTests
{
    [Fact]
    public void Constructor_WithValidValues_CreatesActiveUser()
    {
        // Arrange
        var departmentId = Guid.NewGuid();

        // Act
        var user = new User(
            " Mustafa Yeşilay ",
            " MUSTAFA@EXAMPLE.COM ",
            " example-password-hash ",
            UserRole.Employee,
            departmentId);

        // Assert
        Assert.NotEqual(Guid.Empty, user.Id);
        Assert.Equal("Mustafa Yeşilay", user.FullName);
        Assert.Equal("mustafa@example.com", user.Email);
        Assert.Equal("example-password-hash", user.PasswordHash);
        Assert.Equal(UserRole.Employee, user.Role);
        Assert.Equal(departmentId, user.DepartmentId);
        Assert.True(user.IsActive);
        Assert.NotEqual(default, user.CreatedAt);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void Constructor_WithInvalidEmail_ThrowsArgumentException()
    {
        // Arrange
        var departmentId = Guid.NewGuid();

        // Act
        var action = () => new User(
            "Mustafa Yeşilay",
            "gecersiz-email",
            "example-password-hash",
            UserRole.Employee,
            departmentId);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithEmptyDepartmentId_ThrowsArgumentException()
    {
        // Act
        var action = () => new User(
            "Mustafa Yeşilay",
            "mustafa@example.com",
            "example-password-hash",
            UserRole.Employee,
            Guid.Empty);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithInvalidRole_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var invalidRole = (UserRole)999;

        // Act
        var action = () => new User(
            "Mustafa Yeşilay",
            "mustafa@example.com",
            "example-password-hash",
            invalidRole,
            Guid.NewGuid());

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(action);
    }

    [Fact]
    public void Constructor_WithEmptyPasswordHash_ThrowsArgumentException()
    {
        // Act
        var action = () => new User(
            "Mustafa Yeşilay",
            "mustafa@example.com",
            "   ",
            UserRole.Employee,
            Guid.NewGuid());

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithTooLongFullName_ThrowsArgumentException()
    {
        // Arrange
        var fullName = new string(
            'A',
            User.MaxFullNameLength + 1);

        // Act
        var action = () => new User(
            fullName,
            "mustafa@example.com",
            "example-password-hash",
            UserRole.Employee,
            Guid.NewGuid());

        // Assert
        Assert.Throws<ArgumentException>(action);
    }
}