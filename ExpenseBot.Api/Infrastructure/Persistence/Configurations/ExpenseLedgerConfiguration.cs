using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ExpenseLedgerConfiguration : IEntityTypeConfiguration<ExpenseLedger>
{
    public void Configure(EntityTypeBuilder<ExpenseLedger> builder)
    {
        builder.ToTable("expense_ledgers", table =>
            table.HasCheckConstraint("ck_expense_ledgers_currency", "default_currency ~ '^[A-Z]{3}$'"));

        builder.HasKey(ledger => ledger.Id).HasName("pk_expense_ledgers");

        builder.Property(ledger => ledger.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(ledger => ledger.TelegramChatId).HasColumnName("telegram_chat_id");
        builder.Property(ledger => ledger.TelegramChatType).HasColumnName("telegram_chat_type").HasMaxLength(32);
        builder.Property(ledger => ledger.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(ledger => ledger.DefaultCurrency).HasColumnName("default_currency").HasMaxLength(3);
        builder.Property(ledger => ledger.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(ledger => ledger.UpdatedAtUtc).HasColumnName("updated_at");

        builder.HasIndex(ledger => ledger.TelegramChatId)
            .IsUnique()
            .HasDatabaseName("ux_expense_ledgers_telegram_chat_id");
    }
}
