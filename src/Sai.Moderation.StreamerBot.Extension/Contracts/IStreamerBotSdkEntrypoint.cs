namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IStreamerBotSdkEntrypoint
{
    Task<bool> HandleSdkEventAsync(
        string eventName,
        object? payload,
        CancellationToken cancellationToken = default);
}
