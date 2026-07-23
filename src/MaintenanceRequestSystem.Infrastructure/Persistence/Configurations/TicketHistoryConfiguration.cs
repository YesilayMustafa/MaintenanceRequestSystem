using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class TicketHistoryConfiguration
    : IEntityTypeConfiguration<TicketHistory>
{
    public void Configure(EntityTypeBuilder<TicketHistory> builder)
    {
        builder.ToTable("ticket_histories");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.Id)
            .HasColumnName("id");

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
            .HasMaxLength(1000)
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