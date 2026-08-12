using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class TicketCategoryConfiguration
    : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("ticket_categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Id)
            .HasColumnName("id");

        builder.Property(category => category.Name)
            .HasColumnName("name")
            .HasMaxLength(TicketCategory.MaxNameLength)
            .IsRequired();

        builder.Property(category => category.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(TicketCategory.MaxNormalizedNameLength)
            .IsRequired();

        builder.HasIndex(category => category.NormalizedName)
            .IsUnique();

        builder.Property(category => category.Description)
            .HasColumnName("description")
            .HasMaxLength(TicketCategory.MaxDescriptionLength);

        builder.Property(category => category.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(category => category.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(category => category.UpdatedAt)
            .HasColumnName("updated_at");
    }
}
