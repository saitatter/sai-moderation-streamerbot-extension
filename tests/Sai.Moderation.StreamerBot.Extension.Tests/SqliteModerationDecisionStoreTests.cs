using Sai.Moderation.StreamerBot.Extension.Models;
using Sai.Moderation.StreamerBot.Extension.Options;
using Sai.Moderation.StreamerBot.Extension.Services;

namespace Sai.Moderation.StreamerBot.Extension.Tests;

public sealed class SqliteModerationDecisionStoreTests : IDisposable
{
    private readonly string databasePath;

    public SqliteModerationDecisionStoreTests()
    {
        databasePath = Path.Combine(Path.GetTempPath(), $"sai-moderation-{Guid.NewGuid():N}.db");
    }

    [Fact]
    public async Task SavesAndRetrievesDecision()
    {
        var store = CreateStore();
        var chatEvent = new ChatEvent(
            "m-1",
            "Twitch",
            "c-1",
            "u-1",
            "alice",
            "hello",
            DateTimeOffset.UtcNow);
        var result = new ModerationResult("m-1", ModerationVerdict.Flag, 0.72, "toxicity", "review", 15);

        await store.SaveAsync(chatEvent, result);
        var stored = await store.GetByMessageIdAsync("m-1");

        Assert.NotNull(stored);
        Assert.Equal("alice", stored!.ChatEvent.Username);
        Assert.Equal(ModerationVerdict.Flag, stored.Result.Verdict);
    }

    [Fact]
    public async Task UpsertsExistingDecision()
    {
        var store = CreateStore();
        var chatEvent = new ChatEvent(
            "m-2",
            "Twitch",
            "c-1",
            "u-1",
            "alice",
            "hello",
            DateTimeOffset.UtcNow);

        await store.SaveAsync(chatEvent, new ModerationResult("m-2", ModerationVerdict.Allow, 0.9, "safe", "ok", 10));
        await store.SaveAsync(chatEvent, new ModerationResult("m-2", ModerationVerdict.Block, 0.96, "toxicity", "manual", 10));

        var stored = await store.GetByMessageIdAsync("m-2");

        Assert.NotNull(stored);
        Assert.Equal(ModerationVerdict.Block, stored!.Result.Verdict);
        Assert.Equal("manual", stored.Result.Reason);
    }

    private SqliteModerationDecisionStore CreateStore()
    {
        return new SqliteModerationDecisionStore(new SqliteDecisionStoreOptions { DatabasePath = databasePath });
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
        catch
        {
            // Ignore file cleanup issues in tests.
        }
    }
}
