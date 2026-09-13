using ExpenseBot.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExpenseBot.Api.Infrastructure.Persistence;

public sealed class ExpenseBotDbContext(DbContextOptions<ExpenseBotDbContext> options)
    : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<ExpenseLedger> Ledgers => Set<ExpenseLedger>();
    public DbSet<LedgerMember> LedgerMembers => Set<LedgerMember>();
    public DbSet<ExpenseCategory> Categories => Set<ExpenseCategory>();
    public DbSet<ExpenseDraft> ExpenseDrafts => Set<ExpenseDraft>();
    public DbSet<Expense> Expenses => Set<Expense>();
    public DbSet<ProcessedTelegramUpdate> ProcessedTelegramUpdates => Set<ProcessedTelegramUpdate>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExpenseBotDbContext).Assembly);
    }
}
