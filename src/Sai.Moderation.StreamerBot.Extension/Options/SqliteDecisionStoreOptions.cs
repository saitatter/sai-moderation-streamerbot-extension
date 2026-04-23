namespace Sai.Moderation.StreamerBot.Extension.Options;

public sealed class SqliteDecisionStoreOptions
{
    public string DatabasePath { get; init; } = "sai-moderation.db";
}
