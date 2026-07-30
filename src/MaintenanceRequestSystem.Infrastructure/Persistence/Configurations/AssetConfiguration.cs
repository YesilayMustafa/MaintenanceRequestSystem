using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class AssetConfiguration
    : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.ToTable("assets");

        builder.HasKey(asset => asset.Id);

        builder.Property(asset => asset.Id)
            .HasColumnName("id");

        builder.Property(asset => asset.Name)
            .HasColumnName("name")
            .HasMaxLength(Asset.MaxNameLength)
            .IsRequired();

        builder.Property(asset => asset.SerialNumber)
            .HasColumnName("serial_number")
            .HasMaxLength(Asset.MaxSerialNumberLength)
            .IsRequired();

        builder.HasIndex(asset => asset.SerialNumber)
            .IsUnique();

        builder.Property(asset => asset.Type)
            .HasColumnName("asset_type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(asset => asset.Location)
            .HasColumnName("location")
            .HasMaxLength(Asset.MaxLocationLength);

        builder.Property(asset => asset.DepartmentId)
            .HasColumnName("department_id")
            .IsRequired();

        builder.HasIndex(asset => asset.DepartmentId);

        builder.Property(asset => asset.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(asset => asset.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(asset => asset.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasOne(asset => asset.Department)
            .WithMany(department => department.Assets)
            .HasForeignKey(asset => asset.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}