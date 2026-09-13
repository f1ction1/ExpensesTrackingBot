namespace ExpenseBot.Api.Domain.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public long TelegramUserId { get; set; }
    public required string FirstName { get; set; }
    public string? Username { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<LedgerMember> Memberships { get; set; } = [];
}
