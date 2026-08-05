using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

/// <summary>
/// Ticket history kayıtlarının EF Core tablo, alan ve ilişki eşlemelerini tanımlar.
/// </summary>
public sealed class TicketHistoryConfiguration
    : IEntityTypeConfiguration<TicketHistory>
{
    /// <summary>
    /// TicketHistory entity'sinin kalıcılık modelini yapılandırır.
    /// </summary>
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        builder.ToTable("ticket_histories");

        builder.HasQueryFilter(
            history => !history.Ticket.IsDeleted);

        builder.HasKey(history => history.Id);

        // Kimlik domain constructor'ında üretildiği için EF Core bu kaydı mevcut entity güncellemesi sanmamalıdır.
        builder.Property(history => history.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(history => history.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(history => history.PerformedByUserId)
            .HasColumnName("performed_by_user_id")
            .IsRequired();

        builder.Property(history => history.OldStatus)
            .HasColumnName("old_status")
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(history => history.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(history => history.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(history => history.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(history => history.TicketId);

        builder.HasIndex(history => history.PerformedByUserId);

        builder.HasIndex(history => new
        {
            history.TicketId,
            history.CreatedAt
        });

        builder.HasOne(history => history.Ticket)
            .WithMany(ticket => ticket.Histories)
            .HasForeignKey(history => history.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(history => history.PerformedByUser)
            .WithMany(user => user.TicketHistories)
            .HasForeignKey(history => history.PerformedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
