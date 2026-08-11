using MaintenanceRequestSystem.Infrastructure;
using Scalar.AspNetCore;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.Api.ExceptionHandling;
using MaintenanceRequestSystem.Api.Extensions;
using MaintenanceRequestSystem.Api.OpenApi;
using MaintenanceRequestSystem.Api.Authentication;
using MaintenanceRequestSystem.Api.RateLimiting;
using MaintenanceRequestSystem.Application.Authentication.Models;

const string FrontendCorsPolicy = "FrontendDevelopment";

var builder = WebApplication.CreateBuilder(args);

var allowedOrigins =
    builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql");
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<
    ICurrentUserAccessor,
    CurrentUserAccessor>();
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        FrontendCorsPolicy,
        policy =>
        {
            if (allowedOrigins.Length > 0)
            {
                policy.WithOrigins(allowedOrigins);
            }

            policy
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<
        BearerSecuritySchemeTransformer>();

    options.AddOperationTransformer<
        AuthOperationTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddAccountRateLimiting(builder.Configuration);
builder.Services.AddInfrastructure(
    builder.Configuration,
    builder.Environment.IsDevelopment());

builder.Services.AddJwtAuthentication();

var app = builder.Build();

// Production dahil tüm ortamlarda lifecycle link yapılandırması startup sırasında doğrulanır.
_ = app.Services.GetRequiredService<AccountLifecycleSettings>();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

// Yerel geliştirmede şimdilik kapalı.
// app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
await app.SeedDevelopmentDataAsync();
app.Run();
public partial class Program
{
}
