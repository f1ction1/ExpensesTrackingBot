using ExpenseBot.Api.Application.Abstractions;
using Npgsql;

namespace ExpenseBot.Api.Infrastructure.Persistence;

public sealed class TelegramUpdateGate(NpgsqlDataSource dataSource) : ITelegramUpdateGate
{
    public async Task<bool> TryAcquireAsync(int updateId, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO processed_telegram_updates (update_id, locked_until, completed_at)
            VALUES (@update_id, now() + interval '2 minutes', NULL)
            ON CONFLICT (update_id) DO UPDATE
            SET locked_until = EXCLUDED.locked_until
            WHERE processed_telegram_updates.completed_at IS NULL
              AND processed_telegram_updates.locked_until < now()
            RETURNING update_id;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("update_id", updateId);
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task CompleteAsync(int updateId, CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE processed_telegram_updates
            SET completed_at = now(), locked_until = now()
            WHERE update_id = @update_id;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("update_id", updateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task ReleaseAsync(int updateId, CancellationToken cancellationToken)
    {
        const string sql = """
            DELETE FROM processed_telegram_updates
            WHERE update_id = @update_id AND completed_at IS NULL;
            """;

        await using var command = dataSource.CreateCommand(sql);
        command.Parameters.AddWithValue("update_id", updateId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
