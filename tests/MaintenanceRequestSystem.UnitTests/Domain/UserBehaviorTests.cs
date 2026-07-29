using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed class UserBehaviorTests
{
    [Fact]
    public void UpdateDetails_WithValidValues_UpdatesUser()
    {
        // Arrange
        var originalDepartmentId = Guid.NewGuid();
        var newDepartmentId = Guid.NewGuid();

        var user = CreateUser(originalDepartmentId);

        var beforeUpdate = DateTime.UtcNow;

        // Act
        user.UpdateDetails(
            "  Ahmet Yılmaz Güncel  ",
            "  AHMET.GUNCEL@EXAMPLE.COM  ",
            newDepartmentId);

        // Assert
        Assert.Equal(
            "Ahmet Yılmaz Güncel",
            user.FullName);

        Assert.Equal(
            "ahmet.guncel@example.com",
            user.Email);

        Assert.Equal(
            newDepartmentId,
            user.DepartmentId);

        Assert.NotNull(user.UpdatedAt);

        Assert.InRange(
            user.UpdatedAt.Value,
            beforeUpdate,
            DateTime.UtcNow);
    }

    [Fact]
    public void UpdateDetails_WithInvalidEmail_DoesNotPartiallyUpdateUser()
    {
        // Arrange
        var originalDepartmentId = Guid.NewGuid();

        var user = CreateUser(originalDepartmentId);

        var originalFullName = user.FullName;
        var originalEmail = user.Email;

        // Act
        Assert.Throws<ArgumentException>(
            () => user.UpdateDetails(
                "Değişmemesi Gereken İsim",
                "gecersiz-eposta",
                Guid.NewGuid()));

        // Assert
        Assert.Equal(
            originalFullName,
            user.FullName);

        Assert.Equal(
            originalEmail,
            user.Email);

        Assert.Equal(
            originalDepartmentId,
            user.DepartmentId);

        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void ChangeRole_WithValidRole_ChangesRole()
    {
        // Arrange
        var user = CreateUser();

        var beforeUpdate = DateTime.UtcNow;

        // Act
        user.ChangeRole(UserRole.Technician);

        // Assert
        Assert.Equal(
            UserRole.Technician,
            user.Role);

        Assert.NotNull(user.UpdatedAt);

        Assert.InRange(
            user.UpdatedAt.Value,
            beforeUpdate,
            DateTime.UtcNow);
    }

    [Fact]
    public void ChangeRole_WithInvalidRole_DoesNotChangeCurrentRole()
    {
        // Arrange
        var user = CreateUser();

        var originalRole = user.Role;

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(
            () => user.ChangeRole(
                (UserRole)999));

        // Assert
        Assert.Equal(
            originalRole,
            user.Role);

        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void Deactivate_SetsUserAsInactive()
    {
        // Arrange
        var user = CreateUser();

        // Act
        user.Deactivate();

        // Assert
        Assert.False(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public void Activate_AfterDeactivation_SetsUserAsActive()
    {
        // Arrange
        var user = CreateUser();

        user.Deactivate();

        // Act
        user.Activate();

        // Assert
        Assert.True(user.IsActive);
        Assert.NotNull(user.UpdatedAt);
    }

    private static User CreateUser(
        Guid? departmentId = null)
    {
        return new User(
            "Ahmet Yılmaz",
            "ahmet@example.com",
            "test-password-hash",
            UserRole.Employee,
            departmentId ?? Guid.NewGuid());
    }
}