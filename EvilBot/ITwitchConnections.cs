using TwitchLib.Api;
using TwitchLib.Client;

namespace EvilBot
{
    /// <summary>
    /// Only used for getting TwitchConnections from a connections class.
    /// </summary>
    internal interface ITwitchConnections
    {
        TwitchAPI Api { get; }
        TwitchClient Client { get; }
    }
}