namespace ExpenseBot.Api.Domain.Entities;

public sealed class ExpenseDraft
{
    public Guid LedgerId { get; set; }
    public Guid UserId { get; set; }
    public long? CategoryId { get; set; }
    public int? PromptMessageId { get; set; }
    public int[] FlowMessageIds { get; set; } = [];
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public uint Version { get; set; }

    public LedgerMember Member { get; set; } = null!;
    public ExpenseCategory? Category { get; set; }
}
