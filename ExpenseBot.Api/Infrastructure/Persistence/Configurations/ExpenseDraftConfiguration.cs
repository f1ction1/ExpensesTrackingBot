using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class ExpenseDraftConfiguration : IEntityTypeConfiguration<ExpenseDraft>
{
    public void Configure(EntityTypeBuilder<ExpenseDraft> builder)
    {
        builder.ToTable("expense_drafts");
        builder.HasKey(draft => new { draft.LedgerId, draft.UserId }).HasName("pk_expense_drafts");

        builder.Property(draft => draft.LedgerId).HasColumnName("ledger_id");
        builder.Property(draft => draft.UserId).HasColumnName("user_id");
        builder.Property(draft => draft.CategoryId).HasColumnName("category_id");
        builder.Property(draft => draft.PromptMessageId).HasColumnName("prompt_message_id");
        builder.Property(draft => draft.FlowMessageIds).HasColumnName("flow_message_ids").HasColumnType("integer[]");
        builder.Property(draft => draft.ExpiresAtUtc).HasColumnName("expires_at");
        builder.Property(draft => draft.CreatedAtUtc).HasColumnName("created_at");
        builder.Property(draft => draft.UpdatedAtUtc).HasColumnName("updated_at");
        builder.Property(draft => draft.Version).IsRowVersion();

        builder.HasIndex(draft => draft.ExpiresAtUtc).HasDatabaseName("ix_expense_drafts_expires_at");

        builder.HasOne(draft => draft.Member)
            .WithOne(member => member.Draft)
            .HasForeignKey<ExpenseDraft>(draft => new { draft.LedgerId, draft.UserId })
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_expense_drafts_members");

        builder.HasOne(draft => draft.Category)
            .WithMany()
            .HasForeignKey(draft => new { draft.CategoryId, draft.LedgerId })
            .HasPrincipalKey(category => new { category.Id, category.LedgerId })
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_expense_drafts_categories");
    }
}
