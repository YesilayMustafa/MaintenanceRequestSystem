using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Application.Departments.Interfaces;
using MaintenanceRequestSystem.Application.Departments.Services;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using MaintenanceRequestSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        services.AddScoped<IDepartmentService, DepartmentService>();

        return services;
    }
}