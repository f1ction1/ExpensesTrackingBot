namespace ExpenseBot.Api.Domain.Entities;

public sealed class LedgerMember
{
    public Guid LedgerId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = LedgerMemberRoles.Member;
    public DateTime JoinedAtUtc { get; set; }

    public ExpenseLedger Ledger { get; set; } = null!;
    public AppUser User { get; set; } = null!;
    public ExpenseDraft? Draft { get; set; }
    public ICollection<Expense> Expenses { get; set; } = [];
}

public static class LedgerMemberRoles
{
    public const string Owner = "owner";
    public const string Admin = "admin";
    public const string Member = "member";
}
