using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ProcessedTelegramUpdateConfiguration : IEntityTypeConfiguration<ProcessedTelegramUpdate>
{
    public void Configure(EntityTypeBuilder<ProcessedTelegramUpdate> builder)
    {
        builder.ToTable("processed_telegram_updates");
        builder.HasKey(update => update.UpdateId).HasName("pk_processed_telegram_updates");

        builder.Property(update => update.UpdateId).HasColumnName("update_id").ValueGeneratedNever();
        builder.Property(update => update.LockedUntilUtc).HasColumnName("locked_until");
        builder.Property(update => update.CompletedAtUtc).HasColumnName("completed_at");

        builder.HasIndex(update => update.CompletedAtUtc)
            .HasDatabaseName("ix_processed_telegram_updates_completed_at");
    }
}
