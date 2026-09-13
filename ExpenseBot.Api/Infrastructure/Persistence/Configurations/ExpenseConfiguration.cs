using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.ToTable("expenses", table =>
        {
            table.HasCheckConstraint("ck_expenses_amount", "amount > 0");
            table.HasCheckConstraint("ck_expenses_currency", "currency ~ '^[A-Z]{3}$'");
        });

        builder.HasKey(expense => expense.Id).HasName("pk_expenses");

        builder.Property(expense => expense.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(expense => expense.LedgerId).HasColumnName("ledger_id");
        builder.Property(expense => expense.CreatedByUserId).HasColumnName("created_by_user_id");
        builder.Property(expense => expense.CategoryId).HasColumnName("category_id");
        builder.Property(expense => expense.Amount).HasColumnName("amount").HasPrecision(14, 2);
        builder.Property(expense => expense.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(expense => expense.Description).HasColumnName("description").HasMaxLength(500);
        builder.Property(expense => expense.RawText).HasColumnName("raw_text");
        builder.Property(expense => expense.TelegramMessageId).HasColumnName("telegram_message_id");
        builder.Property(expense => expense.OccurredAtUtc).HasColumnName("occurred_at");
        builder.Property(expense => expense.CreatedAtUtc).HasColumnName("created_at");

        builder.HasIndex(expense => new { expense.LedgerId, expense.TelegramMessageId })
            .IsUnique()
            .HasDatabaseName("ux_expenses_ledger_id_telegram_message_id");

        builder.HasIndex(expense => new { expense.LedgerId, expense.OccurredAtUtc })
            .IsDescending(false, true)
            .HasDatabaseName("ix_expenses_ledger_occurred_at");

        builder.HasIndex(expense => new { expense.LedgerId, expense.CategoryId, expense.OccurredAtUtc })
            .IsDescending(false, false, true)
            .HasDatabaseName("ix_expenses_ledger_category_occurred_at");

        builder.HasOne(expense => expense.Member)
            .WithMany(member => member.Expenses)
            .HasForeignKey(expense => new { expense.LedgerId, expense.CreatedByUserId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expenses_members");

        builder.HasOne(expense => expense.Category)
            .WithMany(category => category.Expenses)
            .HasForeignKey(expense => new { expense.CategoryId, expense.LedgerId })
            .HasPrincipalKey(category => new { category.Id, category.LedgerId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expenses_categories");
    }
}
