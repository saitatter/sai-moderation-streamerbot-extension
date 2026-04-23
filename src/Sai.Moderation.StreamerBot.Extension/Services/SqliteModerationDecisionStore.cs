using System.Text.Json;
using Microsoft.Data.Sqlite;
using Sai.Moderation.StreamerBot.Extension.Contracts;
using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;

namespace Sai.Moderation.StreamerBot.Extension.Services;

public sealed class SqliteModerationDecisionStore : IModerationDecisionStore
{
    private readonly string connectionString;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private bool initialized;

    public SqliteModerationDecisionStore(SqliteDecisionStoreOptions options)
    {
        var databasePath = string.IsNullOrWhiteSpace(options.DatabasePath)
            ? "sai-moderation.db"
            : options.DatabasePath;
        connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath
        }.ToString();
    }

    public async Task SaveAsync(
        ChatEvent chatEvent,
        ModerationResult result,
        CancellationToken cancellationToken = default)
    {
        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var chatEventJson = JsonSerializer.Serialize(chatEvent, JsonOptions);
        var resultJson = JsonSerializer.Serialize(result, JsonOptions);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO moderation_decisions (message_id, chat_event_json, moderation_result_json, updated_at_utc)
            VALUES ($messageId, $chatEventJson, $resultJson, $updatedAtUtc)
            ON CONFLICT(message_id) DO UPDATE SET
                chat_event_json = excluded.chat_event_json,
                moderation_result_json = excluded.moderation_result_json,
                updated_at_utc = excluded.updated_at_utc;
            """;
        command.Parameters.AddWithValue("$messageId", result.MessageId);
        command.Parameters.AddWithValue("$chatEventJson", chatEventJson);
        command.Parameters.AddWithValue("$resultJson", resultJson);
        command.Parameters.AddWithValue("$updatedAtUtc", DateTimeOffset.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<StoredModerationDecision?> GetByMessageIdAsync(
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(messageId)) return null;

        await EnsureSchemaAsync(cancellationToken);
        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT chat_event_json, moderation_result_json
            FROM moderation_decisions
            WHERE message_id = $messageId
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$messageId", messageId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;

        var chatEventJson = reader.GetString(0);
        var resultJson = reader.GetString(1);
        var chatEvent = JsonSerializer.Deserialize<ChatEvent>(chatEventJson, JsonOptions);
        var moderationResult = JsonSerializer.Deserialize<ModerationResult>(resultJson, JsonOptions);
        if (chatEvent is null || moderationResult is null) return null;

        return new StoredModerationDecision(chatEvent, moderationResult);
    }

    private async Task EnsureSchemaAsync(CancellationToken cancellationToken)
    {
        if (initialized) return;

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS moderation_decisions (
                message_id TEXT PRIMARY KEY,
                chat_event_json TEXT NOT NULL,
                moderation_result_json TEXT NOT NULL,
                updated_at_utc TEXT NOT NULL
            );
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
        initialized = true;
    }
}
