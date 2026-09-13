namespace ExpenseBot.Api.Domain.Entities;

public sealed class ExpenseLedger
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public long TelegramChatId { get; set; }
    public required string TelegramChatType { get; set; }
    public required string Name { get; set; }
    public string DefaultCurrency { get; set; } = "PLN";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<LedgerMember> Members { get; set; } = [];
    public ICollection<ExpenseCategory> Categories { get; set; } = [];
}
