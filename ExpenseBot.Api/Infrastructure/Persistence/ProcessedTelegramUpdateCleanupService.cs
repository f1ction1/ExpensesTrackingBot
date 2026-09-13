using Npgsql;

namespace ExpenseBot.Api.Infrastructure.Persistence;

public sealed class ProcessedTelegramUpdateCleanupService(
    NpgsqlDataSource dataSource,
    ILogger<ProcessedTelegramUpdateCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(6);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CleanupInterval);

        do
        {
            try
            {
                const string sql = """
                    DELETE FROM processed_telegram_updates
                    WHERE completed_at < now() - interval '7 days'
                       OR (completed_at IS NULL AND locked_until < now() - interval '1 day');
                    """;

                await using var command = dataSource.CreateCommand(sql);
                var deletedRows = await command.ExecuteNonQueryAsync(stoppingToken);

                if (deletedRows > 0)
                {
                    logger.LogInformation("Deleted {DeletedRows} old Telegram update records.", deletedRows);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to clean old Telegram update records.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
