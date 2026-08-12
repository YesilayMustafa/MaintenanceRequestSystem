namespace MaintenanceRequestSystem.Api.RateLimiting;

public static class AccountRateLimitPolicyNames
{
    public const string Login = "AccountLogin";
    public const string ForgotPassword = "AccountForgotPassword";
    public const string AcceptInvitation = "AccountAcceptInvitation";
    public const string ResetPassword = "AccountResetPassword";
    public const string ChangePassword = "AccountChangePassword";
}
