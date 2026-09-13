namespace ExpenseBot.Api.Domain.Entities;

public sealed class ExpenseCategory
{
    public long Id { get; set; }
    public Guid LedgerId { get; set; }
    public required string Name { get; set; }
    public required string NormalizedName { get; set; }
    public required string Emoji { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ExpenseLedger Ledger { get; set; } = null!;
    public ICollection<Expense> Expenses { get; set; } = [];
}
