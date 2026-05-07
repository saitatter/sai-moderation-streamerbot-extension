namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IStreamerBotChannelPublisher
{
    Task PublishAsync(
        string channel,
        string payload,
        CancellationToken cancellationToken = default);
}
