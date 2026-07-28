using MaintenanceRequestSystem.Infrastructure;
using Scalar.AspNetCore;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.Api.ExceptionHandling;
using MaintenanceRequestSystem.Api.Extensions;
using MaintenanceRequestSystem.Api.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql");
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<
        BearerSecuritySchemeTransformer>();

    options.AddOperationTransformer<
        AuthOperationTransformer>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

// Yerel geliştirmede şimdilik kapalı.
// app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();
await app.SeedDevelopmentDataAsync();
app.Run();
public partial class Program
{
}