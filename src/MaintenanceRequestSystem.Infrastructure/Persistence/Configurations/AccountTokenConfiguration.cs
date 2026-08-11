using MaintenanceRequestSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Configurations;

public sealed class AccountTokenConfiguration
    : IEntityTypeConfiguration<AccountToken>
{
    public void Configure(
        EntityTypeBuilder<AccountToken> builder)
    {
        builder.ToTable("account_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .HasColumnName("id");

        builder.Property(token => token.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(AccountToken.MaxTokenHashLength)
            .IsRequired();

        builder.HasIndex(token => token.TokenHash)
            .IsUnique();

        builder.Property(token => token.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(token => token.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(token => token.UsedAt)
            .HasColumnName("used_at")
            .IsConcurrencyToken();

        builder.Property(token => token.RevokedAt)
            .HasColumnName("revoked_at")
            .IsConcurrencyToken();

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.HasIndex(token => new
        {
            token.UserId,
            token.Type,
            token.UsedAt,
            token.RevokedAt,
            token.ExpiresAt
        });

        builder.HasOne(token => token.User)
            .WithMany(user => user.AccountTokens)
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
