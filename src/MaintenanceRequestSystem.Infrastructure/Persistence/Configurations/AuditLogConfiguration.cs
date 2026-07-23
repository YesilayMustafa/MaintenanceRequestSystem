using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(auditLog => auditLog.Id);

        builder.Property(auditLog => auditLog.Id)
            .HasColumnName("id");

        builder.Property(auditLog => auditLog.PerformedByUserId)
            .HasColumnName("performed_by_user_id")
            .IsRequired();

        builder.Property(auditLog => auditLog.Action)
            .HasColumnName("action")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(auditLog => auditLog.OldValues)
            .HasColumnName("old_values")
            .HasColumnType("jsonb");

        builder.Property(auditLog => auditLog.NewValues)
            .HasColumnName("new_values")
            .HasColumnType("jsonb");

        builder.Property(auditLog => auditLog.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(auditLog => auditLog.PerformedByUserId);

        builder.HasIndex(auditLog => auditLog.CreatedAt);

        builder.HasIndex(auditLog => new
        {
            auditLog.EntityName,
            auditLog.EntityId
        });

        builder.HasOne(auditLog => auditLog.PerformedByUser)
            .WithMany(user => user.AuditLogs)
            .HasForeignKey(auditLog => auditLog.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}