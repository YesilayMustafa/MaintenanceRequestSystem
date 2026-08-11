using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using MaintenanceRequestSystem.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class TicketConfiguration
    : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("tickets");

        builder.HasKey(ticket => ticket.Id);

        builder.Property(ticket => ticket.Id)
            .HasColumnName("id");

        builder.Property(ticket => ticket.TicketNumber)
            .HasColumnName("ticket_number")
            .HasMaxLength(TicketNumberValue.MaxLength)
            .IsRequired();

        builder.HasIndex(ticket => ticket.TicketNumber)
            .IsUnique();

        builder.Property(ticket => ticket.Title)
            .HasColumnName("title")
            .HasMaxLength(Ticket.MaxTitleLength)
            .IsRequired();

        builder.Property(ticket => ticket.Description)
            .HasColumnName("description")
            .HasMaxLength(Ticket.MaxDescriptionLength)
            .IsRequired();

        builder.Property(ticket => ticket.Priority)
            .HasColumnName("priority")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(ticket => ticket.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(ticket => ticket.AssetId)
            .HasColumnName("asset_id")
            .IsRequired();

        builder.Property(ticket => ticket.CreatedByUserId)
            .HasColumnName("created_by_user_id")
            .IsRequired();

        builder.Property(ticket => ticket.AssignedTechnicianId)
            .HasColumnName("assigned_technician_id");

        builder.Property(ticket => ticket.WaitingReason)
            .HasColumnName("waiting_reason")
            .HasMaxLength(Ticket.MaxWaitingReasonLength);

        builder.Property(ticket => ticket.ResolutionDescription)
            .HasColumnName("resolution_description")
            .HasMaxLength(Ticket.MaxResolutionDescriptionLength);

        builder.Property(ticket => ticket.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(ticket => ticket.UpdatedAt)
            .HasColumnName("updated_at");

        builder.Property(ticket => ticket.ResolvedAt)
            .HasColumnName("resolved_at");

        builder.Property(ticket => ticket.ClosedAt)
            .HasColumnName("closed_at");

        builder.HasIndex(ticket => ticket.Status);

        builder.HasIndex(ticket => ticket.Priority);

        builder.HasIndex(ticket => ticket.CreatedAt);

        builder.HasIndex(ticket => ticket.AssetId);

        builder.HasIndex(ticket => ticket.CreatedByUserId);

        builder.HasIndex(ticket => ticket.AssignedTechnicianId);

        builder.HasOne(ticket => ticket.Asset)
            .WithMany(asset => asset.Tickets)
            .HasForeignKey(ticket => ticket.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ticket => ticket.CreatedByUser)
            .WithMany(user => user.CreatedTickets)
            .HasForeignKey(ticket => ticket.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ticket => ticket.AssignedTechnician)
            .WithMany(user => user.AssignedTickets)
            .HasForeignKey(ticket => ticket.AssignedTechnicianId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(ticket => ticket.IsDeleted)
            .HasColumnName("is_deleted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(ticket => ticket.DeletedAt)
            .HasColumnName("deleted_at");

        builder.Property(ticket => ticket.DeletedByUserId)
            .HasColumnName("deleted_by_user_id");

        builder.HasIndex(ticket => ticket.IsDeleted);

        builder.HasQueryFilter(ticket => !ticket.IsDeleted);
    }
}
