using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IStreamerBotCallbackReceiver
{
    Task<bool> ReceiveAsync(
        StreamerBotCallbackEvent callbackEvent,
        CancellationToken cancellationToken = default);
}
