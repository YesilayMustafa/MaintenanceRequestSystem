namespace MaintenanceRequestSystem.Api.RateLimiting;

public sealed class AccountRateLimitOptions
{
    public const string SectionName = "RateLimiting:Account";

    public FixedWindowPolicyOptions Login { get; init; } =
        new(20, 60);

    public FixedWindowPolicyOptions ForgotPassword { get; init; } =
        new(10, 60);

    public FixedWindowPolicyOptions AcceptInvitation { get; init; } =
        new(20, 60);

    public FixedWindowPolicyOptions ResetPassword { get; init; } =
        new(20, 60);

    public FixedWindowPolicyOptions ChangePassword { get; init; } =
        new(10, 60);
}

public sealed record FixedWindowPolicyOptions(
    int PermitLimit,
    int WindowSeconds);
