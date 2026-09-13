namespace ExpenseBot.Api.Application.Abstractions;

public interface ITelegramUpdateGate
{
    Task<bool> TryAcquireAsync(int updateId, CancellationToken cancellationToken);
    Task CompleteAsync(int updateId, CancellationToken cancellationToken);
    Task ReleaseAsync(int updateId, CancellationToken cancellationToken);
}
