namespace ExpenseBot.Api.Domain.Entities;

public sealed class ProcessedTelegramUpdate
{
    public int UpdateId { get; set; }
    public DateTime LockedUntilUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
}
