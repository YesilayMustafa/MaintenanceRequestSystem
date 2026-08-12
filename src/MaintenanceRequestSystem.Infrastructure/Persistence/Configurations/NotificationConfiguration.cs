using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration
    : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id)
            .HasColumnName("id");
        builder.Property(notification => notification.UserId)
            .HasColumnName("user_id")
            .IsRequired();
        builder.Property(notification => notification.TicketId)
            .HasColumnName("ticket_id");
        builder.Property(notification => notification.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();
        builder.Property(notification => notification.Title)
            .HasColumnName("title")
            .HasMaxLength(Notification.MaxTitleLength)
            .IsRequired();
        builder.Property(notification => notification.Message)
            .HasColumnName("message")
            .HasMaxLength(Notification.MaxMessageLength)
            .IsRequired();
        builder.Property(notification => notification.IsRead)
            .HasColumnName("is_read")
            .IsRequired();
        builder.Property(notification => notification.ReadAt)
            .HasColumnName("read_at");
        builder.Property(notification => notification.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasOne(notification => notification.User)
            .WithMany(user => user.Notifications)
            .HasForeignKey(notification => notification.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(notification => notification.Ticket)
            .WithMany(ticket => ticket.Notifications)
            .HasForeignKey(notification => notification.TicketId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(notification => new
            {
                notification.UserId,
                notification.IsRead,
                notification.CreatedAt
            });
        builder.HasIndex(notification => new
            {
                notification.UserId,
                notification.CreatedAt
            });
    }
}
