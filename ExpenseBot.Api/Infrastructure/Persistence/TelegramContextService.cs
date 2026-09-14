using ExpenseBot.Api.Application.Abstractions;
using ExpenseBot.Api.Domain;
using Npgsql;

namespace ExpenseBot.Api.Infrastructure.Persistence;

public sealed class TelegramContextService(NpgsqlDataSource dataSource) : ITelegramContextService
{
    public async Task<TelegramContextIds> GetOrCreateAsync(
        TelegramActor actor,
        TelegramChatDescriptor chat,
        CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var userId = await UpsertUserAsync(connection, transaction, actor, cancellationToken);
        var ledgerId = await UpsertLedgerAsync(connection, transaction, chat, cancellationToken);

        await UpsertMembershipAsync(connection, transaction, ledgerId, userId, cancellationToken);
        //await SeedCategoriesAsync(connection, transaction, ledgerId, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new TelegramContextIds(userId, ledgerId);
    }

    private static async Task<Guid> UpsertUserAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TelegramActor actor,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO users (id, telegram_user_id, first_name, username, created_at, updated_at)
            VALUES (@id, @telegram_user_id, @first_name, @username, now(), now())
            ON CONFLICT (telegram_user_id) DO UPDATE
            SET first_name = EXCLUDED.first_name,
                username = EXCLUDED.username,
                updated_at = now()
            RETURNING id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("telegram_user_id", actor.TelegramUserId);
        command.Parameters.AddWithValue("first_name", actor.FirstName);
        command.Parameters.AddWithValue("username", (object?)actor.Username ?? DBNull.Value);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("PostgreSQL did not return the user ID."));
    }

    private static async Task<Guid> UpsertLedgerAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        TelegramChatDescriptor chat,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO expense_ledgers
                (id, telegram_chat_id, telegram_chat_type, name, default_currency, created_at, updated_at)
            VALUES (@id, @telegram_chat_id, @telegram_chat_type, @name, 'PLN', now(), now())
            ON CONFLICT (telegram_chat_id) DO UPDATE
            SET telegram_chat_type = EXCLUDED.telegram_chat_type,
                name = EXCLUDED.name,
                updated_at = now()
            RETURNING id;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("id", Guid.CreateVersion7());
        command.Parameters.AddWithValue("telegram_chat_id", chat.TelegramChatId);
        command.Parameters.AddWithValue("telegram_chat_type", chat.ChatType);
        command.Parameters.AddWithValue("name", chat.Name);

        return (Guid)(await command.ExecuteScalarAsync(cancellationToken)
            ?? throw new InvalidOperationException("PostgreSQL did not return the ledger ID."));
    }

    private static async Task UpsertMembershipAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid ledgerId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO ledger_members (ledger_id, user_id, role, joined_at)
            VALUES (@ledger_id, @user_id, 'member', now())
            ON CONFLICT (ledger_id, user_id) DO NOTHING;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("ledger_id", ledgerId);
        command.Parameters.AddWithValue("user_id", userId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task SeedCategoriesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Guid ledgerId,
        CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO categories
                (ledger_id, name, normalized_name, emoji, sort_order, created_at)
            SELECT @ledger_id, seed.name, seed.normalized_name, seed.emoji, seed.sort_order, now()
            FROM unnest(
                @names::text[],
                @normalized_names::text[],
                @emojis::text[],
                @sort_orders::integer[])
                AS seed(name, normalized_name, emoji, sort_order)
            ON CONFLICT (ledger_id, normalized_name) DO NOTHING;
            """;

        await using var command = new NpgsqlCommand(sql, connection, transaction);
        command.Parameters.AddWithValue("ledger_id", ledgerId);
        command.Parameters.AddWithValue("names", DefaultExpenseCategories.All.Select(category => category.Name).ToArray());
        command.Parameters.AddWithValue(
            "normalized_names",
            DefaultExpenseCategories.All.Select(category => category.NormalizedName).ToArray());
        command.Parameters.AddWithValue("emojis", DefaultExpenseCategories.All.Select(category => category.Emoji).ToArray());
        command.Parameters.AddWithValue("sort_orders", DefaultExpenseCategories.All.Select(category => category.SortOrder).ToArray());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
