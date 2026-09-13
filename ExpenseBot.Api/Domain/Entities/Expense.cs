namespace ExpenseBot.Api.Domain.Entities;

public sealed class Expense
{
    public long Id { get; set; }
    public Guid LedgerId { get; set; }
    public Guid CreatedByUserId { get; set; }
    public long CategoryId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public string? Description { get; set; }
    public required string RawText { get; set; }
    public int TelegramMessageId { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public LedgerMember Member { get; set; } = null!;
    public ExpenseCategory Category { get; set; } = null!;
}
