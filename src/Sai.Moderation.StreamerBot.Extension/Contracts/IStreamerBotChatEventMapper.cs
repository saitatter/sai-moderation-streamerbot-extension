using Sai.Moderation.StreamerBot.Extension.Models;

namespace Sai.Moderation.StreamerBot.Extension.Contracts;

public interface IStreamerBotChatEventMapper
{
    bool TryMap(string rawJson, out ChatEvent? chatEvent);
}

