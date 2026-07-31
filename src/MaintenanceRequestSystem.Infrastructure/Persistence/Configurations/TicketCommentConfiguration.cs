using System;
using System.Collections.Generic;
using System.Text;

using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class TicketCommentConfiguration
    : IEntityTypeConfiguration<TicketComment>
{
    public void Configure(EntityTypeBuilder<TicketComment> builder)
    {
        builder.ToTable("ticket_comments");

        builder.HasKey(comment => comment.Id);

        builder.Property(comment => comment.Id)
            .HasColumnName("id");

        builder.Property(comment => comment.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(comment => comment.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(comment => comment.Content)
            .HasColumnName("content")
            .HasMaxLength(TicketComment.MaxContentLength)
            .IsRequired();

        builder.Property(comment => comment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(comment => comment.TicketId);

        builder.HasIndex(comment => comment.UserId);

        builder.HasIndex(comment => new
        {
            comment.TicketId,
            comment.CreatedAt
        });

        builder.HasOne(comment => comment.Ticket)
            .WithMany(ticket => ticket.Comments)
            .HasForeignKey(comment => comment.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(comment => comment.User)
            .WithMany(user => user.Comments)
            .HasForeignKey(comment => comment.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}  