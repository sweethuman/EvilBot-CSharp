using TwitchLib.Api;
using TwitchLib.Client;

namespace EvilBot
{
    public interface ITwitchChatBot
    {
        TwitchClient Client { get; }
        TwitchAPI Api { get; }

        void Connect();

        void Disconnect();
    }
}