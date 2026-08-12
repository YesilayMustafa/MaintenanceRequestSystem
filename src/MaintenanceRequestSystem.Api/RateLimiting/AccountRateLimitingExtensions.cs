using System.IdentityModel.Tokens.Jwt;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace MaintenanceRequestSystem.Api.RateLimiting;

public static class AccountRateLimitingExtensions
{
    public static IServiceCollection AddAccountRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRateLimiter(options =>
        {
            var settings =
                configuration
                    .GetSection(AccountRateLimitOptions.SectionName)
                    .Get<AccountRateLimitOptions>()
                ?? new AccountRateLimitOptions();

            Validate(settings);

            options.RejectionStatusCode =
                StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, cancellationToken) =>
            {
                var problemDetailsService =
                    context.HttpContext.RequestServices
                        .GetRequiredService<IProblemDetailsService>();

                context.HttpContext.Response.StatusCode =
                    StatusCodes.Status429TooManyRequests;

                await problemDetailsService.TryWriteAsync(
                    new ProblemDetailsContext
                    {
                        HttpContext = context.HttpContext,
                        ProblemDetails = new ProblemDetails
                        {
                            Status = StatusCodes.Status429TooManyRequests,
                            Title = "Çok fazla istek",
                            Detail = "Çok fazla deneme yapıldı. Lütfen daha sonra tekrar deneyin.",
                            Instance = context.HttpContext.Request.Path
                        }
                    });
            };

            AddIpPolicy(
                options,
                AccountRateLimitPolicyNames.Login,
                settings.Login);

            AddIpPolicy(
                options,
                AccountRateLimitPolicyNames.ForgotPassword,
                settings.ForgotPassword);

            AddIpPolicy(
                options,
                AccountRateLimitPolicyNames.AcceptInvitation,
                settings.AcceptInvitation);

            AddIpPolicy(
                options,
                AccountRateLimitPolicyNames.ResetPassword,
                settings.ResetPassword);

            options.AddPolicy(
                AccountRateLimitPolicyNames.ChangePassword,
                httpContext =>
                {
                    var userId = httpContext.User.FindFirst(
                        JwtRegisteredClaimNames.Sub)?.Value;

                    return CreatePartition(
                        !string.IsNullOrWhiteSpace(userId)
                            ? $"user:{userId}"
                            : $"ip:{GetRemoteIp(httpContext)}",
                        settings.ChangePassword);
                });
        });

        return services;
    }

    private static void AddIpPolicy(
        RateLimiterOptions options,
        string policyName,
        FixedWindowPolicyOptions policy)
    {
        options.AddPolicy(
            policyName,
            httpContext => CreatePartition(
                $"ip:{GetRemoteIp(httpContext)}",
                policy));
    }

    private static RateLimitPartition<string> CreatePartition(
        string key,
        FixedWindowPolicyOptions policy)
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromSeconds(
                    policy.WindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }

    private static string GetRemoteIp(HttpContext httpContext)
    {
        return httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }

    private static void Validate(AccountRateLimitOptions settings)
    {
        var policies = new[]
        {
            settings.Login,
            settings.ForgotPassword,
            settings.AcceptInvitation,
            settings.ResetPassword,
            settings.ChangePassword
        };

        if (policies.Any(policy =>
                policy.PermitLimit < 1 ||
                policy.WindowSeconds < 1))
        {
            throw new InvalidOperationException(
                "Account rate-limit değerleri pozitif olmalıdır.");
        }
    }
}
