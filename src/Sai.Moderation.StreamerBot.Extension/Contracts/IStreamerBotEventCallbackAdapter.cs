namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IStreamerBotEventCallbackAdapter
{
    Task<bool> HandleIncomingEventAsync(
        string eventName,
        string rawJson,
        CancellationToken cancellationToken = default);
}
