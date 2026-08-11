using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;

namespace MaintenanceRequestSystem.UnitTests.Domain;

public sealed class AccountStatusTests
{
    [Fact]
    public void ExistingUser_IsActiveAndOperational()
    {
        var user = CreateActiveUser();

        Assert.True(user.IsOperational);
        Assert.Equal(AccountStatus.Active, user.AccountStatus);
    }

    [Fact]
    public void InvitedUser_IsPendingAndNotOperational()
    {
        var user = User.CreateInvited(
            "Pending User",
            "pending@example.com",
            UserRole.Technician,
            Guid.NewGuid());

        Assert.False(user.IsOperational);
        Assert.Equal(
            AccountStatus.PendingInvitation,
            user.AccountStatus);
    }

    [Fact]
    public void InactiveUser_IsInactiveAndNotOperational()
    {
        var user = CreateActiveUser();
        user.Deactivate();

        Assert.False(user.IsOperational);
        Assert.Equal(AccountStatus.Inactive, user.AccountStatus);
    }

    private static User CreateActiveUser()
    {
        return new User(
            "Active User",
            "active@example.com",
            "password-hash",
            UserRole.Employee,
            Guid.NewGuid());
    }
}
