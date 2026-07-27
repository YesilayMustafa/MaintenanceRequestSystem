using MaintenanceRequestSystem.Infrastructure;
using Scalar.AspNetCore;
using MaintenanceRequestSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("postgresql");
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
// Şimdilik yalnızca HTTP kullandığımız için kapatıyoruz.
// app.UseHttpsRedirection();

app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();

app.Run();