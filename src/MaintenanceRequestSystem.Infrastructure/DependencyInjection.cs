
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

namespace MaintenanceRequestSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "DefaultConnection bağlantı bilgisi bulunamadı.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPasswordHashService, PasswordHashService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<ITicketRepository, TicketRepository>();
        services.AddScoped<ITicketQueryService, TicketQueryService>();
        services.AddScoped<ITicketCreationService, TicketCreationService>();
        services.AddScoped<ITicketAssignmentService, TicketAssignmentService>();
        services.AddScoped<
            ITicketTechnicianLifecycleService,
            TicketTechnicianLifecycleService>();
        services.AddScoped<ITicketService, TicketService>();
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
