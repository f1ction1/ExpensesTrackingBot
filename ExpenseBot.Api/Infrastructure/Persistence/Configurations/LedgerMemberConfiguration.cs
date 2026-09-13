using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ExpenseBot.Api.Infrastructure.Persistence.Configurations;

public sealed class LedgerMemberConfiguration : IEntityTypeConfiguration<LedgerMember>
{
    public void Configure(EntityTypeBuilder<LedgerMember> builder)
    {
        builder.ToTable("ledger_members", table =>
            table.HasCheckConstraint("ck_ledger_members_role", "role IN ('owner', 'admin', 'member')"));

        builder.HasKey(member => new { member.LedgerId, member.UserId }).HasName("pk_ledger_members");

        builder.Property(member => member.LedgerId).HasColumnName("ledger_id");
        builder.Property(member => member.UserId).HasColumnName("user_id");
        builder.Property(member => member.Role).HasColumnName("role").HasMaxLength(16);
        builder.Property(member => member.JoinedAtUtc).HasColumnName("joined_at");

        builder.HasOne(member => member.Ledger)
            .WithMany(ledger => ledger.Members)
            .HasForeignKey(member => member.LedgerId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_ledger_members_ledgers");

        builder.HasOne(member => member.User)
            .WithMany(user => user.Memberships)
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_ledger_members_users");
    }
}
