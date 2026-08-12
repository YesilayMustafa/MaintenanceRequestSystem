using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class TicketAttachmentConfiguration
    : IEntityTypeConfiguration<TicketAttachment>
{
    public void Configure(EntityTypeBuilder<TicketAttachment> builder)
    {
        builder.ToTable("ticket_attachments");

        builder.HasQueryFilter(attachment => !attachment.Ticket.IsDeleted);

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .HasColumnName("id");

        builder.Property(attachment => attachment.TicketId)
            .HasColumnName("ticket_id")
            .IsRequired();

        builder.Property(attachment => attachment.UploadedByUserId)
            .HasColumnName("uploaded_by_user_id")
            .IsRequired();

        builder.Property(attachment => attachment.OriginalFileName)
            .HasColumnName("original_file_name")
            .HasMaxLength(TicketAttachment.MaxOriginalFileNameLength)
            .IsRequired();

        builder.Property(attachment => attachment.StorageKey)
            .HasColumnName("storage_key")
            .HasMaxLength(TicketAttachment.MaxStorageKeyLength)
            .IsRequired();

        builder.Property(attachment => attachment.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(TicketAttachment.MaxContentTypeLength)
            .IsRequired();

        builder.Property(attachment => attachment.SizeBytes)
            .HasColumnName("size_bytes")
            .IsRequired();

        builder.Property(attachment => attachment.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(attachment => attachment.UploadedByUserId);

        builder.HasIndex(attachment => new
        {
            attachment.TicketId,
            attachment.CreatedAt
        });

        builder.HasOne(attachment => attachment.Ticket)
            .WithMany(ticket => ticket.Attachments)
            .HasForeignKey(attachment => attachment.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(attachment => attachment.UploadedByUser)
            .WithMany(user => user.UploadedAttachments)
            .HasForeignKey(attachment => attachment.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
