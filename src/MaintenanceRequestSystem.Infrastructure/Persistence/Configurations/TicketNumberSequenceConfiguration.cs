using MaintenanceRequestSystem.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class TicketNumberSequenceConfiguration
    : IEntityTypeConfiguration<TicketNumberSequence>
{
    public void Configure(
        EntityTypeBuilder<TicketNumberSequence> builder)
    {
        builder.ToTable("ticket_number_sequences");

        builder.HasKey(sequence => sequence.Year);

        builder.Property(sequence => sequence.Year)
            .HasColumnName("year")
            .ValueGeneratedNever();

        builder.Property(sequence => sequence.LastValue)
            .HasColumnName("last_value")
            .IsRequired();
    }
}
