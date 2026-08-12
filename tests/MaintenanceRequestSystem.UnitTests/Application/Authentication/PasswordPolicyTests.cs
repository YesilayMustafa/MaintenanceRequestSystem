using MaintenanceRequestSystem.Application.Authentication.Services;
using MaintenanceRequestSystem.Application.Common.Exceptions;

namespace MaintenanceRequestSystem.UnitTests.Application.Authentication;

public sealed class PasswordPolicyTests
{
    [Fact]
    public void EnsureValid_WithLiteralLeadingAndTrailingSpaces_DoesNotTrim()
    {
        PasswordPolicy.EnsureValid(" 123456 ");
    }

    [Fact]
    public void EnsureValid_WithShortPassword_ThrowsValidationException()
    {
        Assert.Throws<RequestValidationException>(
            () => PasswordPolicy.EnsureValid("1234567"));
    }

    [Fact]
    public void EnsureValid_WithPasswordLongerThanMaximum_ThrowsValidationException()
    {
        Assert.Throws<RequestValidationException>(
            () => PasswordPolicy.EnsureValid(new string('x', 129)));
    }
}
