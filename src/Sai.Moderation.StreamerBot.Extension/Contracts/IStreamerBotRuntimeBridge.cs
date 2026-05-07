namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IStreamerBotRuntimeBridge
{
    Task<bool> ProcessRawChatEventAsync(
        string rawJson,
        CancellationToken cancellationToken = default);
}
