
using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Infrastructure.Authentication;
using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Departments.Services;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MaintenanceRequestSystem.Application.Authentication.Services;
using MaintenanceRequestSystem.Application.Users.Interfaces;
using MaintenanceRequestSystem.Application.Users.Services;
using MaintenanceRequestSystem.Application.Assets.Interfaces;
using MaintenanceRequestSystem.Application.Assets.Services;
using MaintenanceRequestSystem.Application.Tickets.Interfaces;
using MaintenanceRequestSystem.Application.Tickets.Services;
using MaintenanceRequestSystem.Application.TicketComments.Interfaces;
using MaintenanceRequestSystem.Application.TicketComments.Services;
using MaintenanceRequestSystem.Application.AuditLogs.Interfaces;
using MaintenanceRequestSystem.Application.AuditLogs.Services;
using MaintenanceRequestSystem.Application.Authentication.Models;
using MaintenanceRequestSystem.Infrastructure.Email;
using MaintenanceRequestSystem.Application.Dashboard.Interfaces;
using MaintenanceRequestSystem.Application.Dashboard.Services;

namespace MaintenanceRequestSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection bağlantı bilgisi bulunamadı.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddSingleton<IAccountTokenGenerator, AccountTokenGenerator>();
        services.AddScoped<IAccountTokenRepository, AccountTokenRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketNumberGenerator, TicketNumberGenerator>();
        services.AddScoped<IDashboardRepository, DashboardRepository>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<ITicketCreationService, TicketCreationService>();
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
        services.AddScoped<
            ITicketTechnicianLifecycleService,
            TicketTechnicianLifecycleService>();
        services.AddScoped<ITicketCompletionService, TicketCompletionService>();
        services.AddScoped<
            ITicketAdministrationService,
            TicketAdministrationService>();
        services.AddScoped<
            IAuditLogRepository,
            AuditLogRepository>();

        services.AddScoped<
            IAuditLogService,
            AuditLogService>();

        services.AddScoped<
            ITicketCommentRepository,
            TicketCommentRepository>();

        services.AddScoped<
            ITicketCommentService,
            TicketCommentService>();

        services.AddScoped<
            IAuthenticationService,
            AuthenticationService>();

        services.AddScoped<
            IAccountLifecycleService,
            AccountLifecycleService>();

        AddAccountLifecycleConfiguration(
            services,
            configuration,
            isDevelopment);

        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddOptions<JwtOptions>()
    .Bind(
        configuration.GetSection(
            JwtOptions.SectionName))
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT Issuer bilgisi bulunamadı.")
    .Validate(
        options =>
            !string.IsNullOrWhiteSpace(options.Audience),
        "JWT Audience bilgisi bulunamadı.")
    .Validate(
        options =>
            IsValidSigningKey(options.SigningKey),
        "JWT imza anahtarı geçersiz veya çok kısa.")
    .Validate(
        options =>
            options.ExpirationMinutes > 0 &&
            options.ExpirationMinutes <= 1440,
        "JWT geçerlilik süresi 1 ile 1440 dakika arasında olmalıdır.")
    .ValidateOnStart();

        services.AddScoped<IJwtTokenService, JwtTokenService>();


        return services;
    }

    private static void AddAccountLifecycleConfiguration(
        IServiceCollection services,
        IConfiguration configuration,
        bool isDevelopment)
    {
        services.AddSingleton(_ =>
            CreateAccountLifecycleSettings(configuration));

        var emailOptions =
            configuration
                .GetSection(EmailDeliveryOptions.SectionName)
                .Get<EmailDeliveryOptions>()
            ?? new EmailDeliveryOptions();

        ValidateEmailOptions(emailOptions, isDevelopment);
        services.AddSingleton(emailOptions);

        if (isDevelopment)
        {
            services.AddSingleton<
                IEmailSender,
                DevelopmentFileEmailSender>();
        }
        else
        {
            services.AddSingleton<
                IEmailSender,
                SmtpEmailSender>();
        }
    }

    private static AccountLifecycleSettings
        CreateAccountLifecycleSettings(
            IConfiguration configuration)
    {
        var invitationExpirationHours =
            configuration.GetValue<int?>(
                "AccountLifecycle:InvitationExpirationHours")
            ?? 24;

        var passwordResetExpirationMinutes =
            configuration.GetValue<int?>(
                "AccountLifecycle:PasswordResetExpirationMinutes")
            ?? 60;

        var frontendBaseUrl = configuration["Frontend:BaseUrl"];

        if (string.IsNullOrWhiteSpace(frontendBaseUrl))
        {
            throw new InvalidOperationException(
                "Frontend:BaseUrl configuration değeri bulunamadı.");
        }

        return new AccountLifecycleSettings(
            TimeSpan.FromHours(invitationExpirationHours),
            TimeSpan.FromMinutes(passwordResetExpirationMinutes),
            frontendBaseUrl);
    }

    private static void ValidateEmailOptions(
        EmailDeliveryOptions options,
        bool isDevelopment)
    {
        if (isDevelopment)
        {
            if (!string.Equals(
                    options.Mode,
                    "DevelopmentFile",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Development ortamında Email:Mode DevelopmentFile olmalıdır.");
            }

            return;
        }

        if (!string.Equals(
                options.Mode,
                "Smtp",
                StringComparison.OrdinalIgnoreCase) ||
            string.IsNullOrWhiteSpace(options.Host) ||
            options.Port is < 1 or > 65535 ||
            string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException(
                "Production SMTP configuration eksik veya geçersiz.");
        }

        if (string.IsNullOrWhiteSpace(options.Username) !=
            string.IsNullOrWhiteSpace(options.Password))
        {
            throw new InvalidOperationException(
                "SMTP kullanıcı adı ve parola birlikte tanımlanmalıdır.");
        }
    }

    private static bool IsValidSigningKey(
    string signingKey)
    {
        if (string.IsNullOrWhiteSpace(signingKey))
        {
            return false;
        }

        try
        {
            var keyBytes =
                Convert.FromBase64String(signingKey);

            return keyBytes.Length >= 32;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
