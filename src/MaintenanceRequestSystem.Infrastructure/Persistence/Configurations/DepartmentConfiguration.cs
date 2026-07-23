using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class DepartmentConfiguration
    : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(department => department.Id);

        builder.Property(department => department.Id)
            .HasColumnName("id");

        builder.Property(department => department.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(department => department.Name)
            .IsUnique();

        builder.Property(department => department.Description)
            .HasColumnName("description")
            .HasMaxLength(500);

        builder.Property(department => department.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(department => department.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(department => department.UpdatedAt)
            .HasColumnName("updated_at");
    }
}