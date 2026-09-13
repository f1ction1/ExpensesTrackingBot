using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(user => user.Id).HasName("pk_users");

        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(user => user.TelegramUserId).HasColumnName("telegram_user_id");
        builder.Property(user => user.FirstName).HasColumnName("first_name").HasMaxLength(255);
        builder.Property(user => user.Username).HasColumnName("username").HasMaxLength(64);
        builder.Property(user => user.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(user => user.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasIndex(user => user.TelegramUserId)
            .IsUnique()
            .HasDatabaseName("ux_users_telegram_user_id");
    }
}
