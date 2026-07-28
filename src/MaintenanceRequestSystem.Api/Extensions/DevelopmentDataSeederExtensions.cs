using MaintenanceRequestSystem.Application.Authentication.Interfaces;
using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.Enums;
using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MaintenanceRequestSystem.Api.Extensions;

public static class DevelopmentDataSeederExtensions
{
    public static async Task SeedDevelopmentDataAsync(
    this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return;
        }

        using var scope = app.Services.CreateScope();

        var context =
            scope.ServiceProvider
                .GetRequiredService<ApplicationDbContext>();

        var passwordHashService =
            scope.ServiceProvider
                .GetRequiredService<IPasswordHashService>();

        var configuration =
            scope.ServiceProvider
                .GetRequiredService<IConfiguration>();

        var adminEmail =
            configuration["SeedAdmin:Email"]?
                .Trim()
                .ToLowerInvariant();

        var adminPassword =
            configuration["SeedAdmin:Password"];

        var employeeEmail =
            configuration["SeedEmployee:Email"]?
                .Trim()
                .ToLowerInvariant();

        var employeePassword =
            configuration["SeedEmployee:Password"];

        if (string.IsNullOrWhiteSpace(adminEmail) ||
            string.IsNullOrWhiteSpace(adminPassword) ||
            string.IsNullOrWhiteSpace(employeeEmail) ||
            string.IsNullOrWhiteSpace(employeePassword))
        {
            throw new InvalidOperationException(
                "Development kullanıcı bilgileri bulunamadı.");
        }

        const string adminDepartmentName = "Sistem Yönetimi";

        var adminDepartment =
            await context.Departments.FirstOrDefaultAsync(
                department =>
                    department.Name == adminDepartmentName);

        if (adminDepartment is null)
        {
            adminDepartment = new Department(
                adminDepartmentName,
                "Sistem yöneticilerinin bağlı olduğu departman.");

            await context.Departments.AddAsync(adminDepartment);
        }

        var adminExists =
            await context.Users.AnyAsync(
                user => user.Email == adminEmail);

        if (!adminExists)
        {
            var adminPasswordHash =
                passwordHashService.HashPassword(adminPassword);

            var adminUser = new User(
                "Sistem Yöneticisi",
                adminEmail,
                adminPasswordHash,
                UserRole.Admin,
                adminDepartment.Id);

            await context.Users.AddAsync(adminUser);
        }

        const string employeeDepartmentName = "Genel Kullanıcılar";

        var employeeDepartment =
            await context.Departments.FirstOrDefaultAsync(
                department =>
                    department.Name == employeeDepartmentName);

        if (employeeDepartment is null)
        {
            employeeDepartment = new Department(
                employeeDepartmentName,
                "Standart çalışan kullanıcıların bağlı olduğu departman.");

            await context.Departments.AddAsync(employeeDepartment);
        }

        var employeeExists =
            await context.Users.AnyAsync(
                user => user.Email == employeeEmail);

        if (!employeeExists)
        {
            var employeePasswordHash =
                passwordHashService.HashPassword(employeePassword);

            var employeeUser = new User(
                "Test Çalışanı",
                employeeEmail,
                employeePasswordHash,
                UserRole.Employee,
                employeeDepartment.Id);

            await context.Users.AddAsync(employeeUser);
        }

        await context.SaveChangesAsync();
    }
}