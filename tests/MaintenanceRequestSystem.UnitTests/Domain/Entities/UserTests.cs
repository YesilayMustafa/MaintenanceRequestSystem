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
        Assert.Equal(user.CreatedAt, user.InvitationAcceptedAt);
        Assert.Equal(1, user.SecurityVersion);
        Assert.Null(user.UpdatedAt);
    }

    [Fact]
    public void CreateInvited_WithValidValues_CreatesPendingUser()
    {
        // Act
        var user = User.CreateInvited(
            "Davetli Kullanıcı",
            "DAVETLI@EXAMPLE.COM",
            UserRole.Employee,
            Guid.NewGuid());

        // Assert
        Assert.True(user.IsActive);
        Assert.Null(user.PasswordHash);
        Assert.Null(user.InvitationAcceptedAt);
        Assert.Equal(1, user.SecurityVersion);
        Assert.Equal("davetli@example.com", user.Email);
    }

    [Fact]
    public void AcceptInvitation_WhenPending_SetsPasswordAndAcceptedAt()
    {
        // Arrange
        var user = User.CreateInvited(
            "Davetli Kullanıcı",
            "davetli@example.com",
            UserRole.Employee,
            Guid.NewGuid());

        var beforeAccept = DateTime.UtcNow;

        // Act
        user.AcceptInvitation("new-password-hash");

        // Assert
        Assert.Equal("new-password-hash", user.PasswordHash);
        Assert.NotNull(user.InvitationAcceptedAt);
        Assert.InRange(
            user.InvitationAcceptedAt.Value,
            beforeAccept,
            DateTime.UtcNow);
        Assert.Equal(2, user.SecurityVersion);
    }

    [Fact]
    public void AcceptInvitation_WhenAlreadyAccepted_ThrowsInvalidOperationException()
    {
        // Arrange
        var user = new User(
            "Mevcut Kullanıcı",
            "mevcut@example.com",
            "existing-password-hash",
            UserRole.Employee,
            Guid.NewGuid());

        // Act
        var action = () =>
            user.AcceptInvitation("new-password-hash");

        // Assert
        Assert.Throws<InvalidOperationException>(action);
        Assert.Equal("existing-password-hash", user.PasswordHash);
        Assert.Equal(1, user.SecurityVersion);
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
