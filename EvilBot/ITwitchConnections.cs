using TwitchLib.Api;
using TwitchLib.Client;

namespace EvilBot
{
    internal interface ITwitchConnections
    {
        TwitchAPI Api { get; }
        TwitchClient Client { get; }
    }
}