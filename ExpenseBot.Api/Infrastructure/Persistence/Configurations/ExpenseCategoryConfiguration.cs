using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> builder)
    {
        builder.ToTable("categories");
        builder.HasKey(category => category.Id).HasName("pk_categories");
        builder.HasAlternateKey(category => new { category.Id, category.LedgerId })
            .HasName("ak_categories_id_ledger_id");

        builder.Property(category => category.Id).HasColumnName("id").UseIdentityAlwaysColumn();
        builder.Property(category => category.LedgerId).HasColumnName("ledger_id");
        builder.Property(category => category.Name).HasColumnName("name").HasMaxLength(100);
        builder.Property(category => category.NormalizedName).HasColumnName("normalized_name").HasMaxLength(100);
        builder.Property(category => category.Emoji).HasColumnName("emoji").HasMaxLength(16);
        builder.Property(category => category.SortOrder).HasColumnName("sort_order");
        builder.Property(category => category.CreatedAtUtc).HasColumnName("created_at");

        builder.HasIndex(category => new { category.LedgerId, category.NormalizedName })
            .IsUnique()
            .HasDatabaseName("ux_categories_ledger_id_normalized_name");

        builder.HasIndex(category => new { category.LedgerId, category.SortOrder, category.Id })
            .HasDatabaseName("ix_categories_ledger_sort_order");

        builder.HasOne(category => category.Ledger)
            .WithMany(ledger => ledger.Categories)
            .HasForeignKey(category => category.LedgerId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_categories_ledgers");
    }
}
