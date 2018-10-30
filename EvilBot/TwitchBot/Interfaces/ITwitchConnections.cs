using TwitchLib.Api;
using TwitchLib.Client;

namespace EvilBot.TwitchBot.Interfaces
{
    /// <summary>
    /// Only used for getting TwitchConnections from a connections class.
    /// </summary>
    public interface ITwitchConnections
    {
        TwitchAPI Api { get; }
        TwitchClient Client { get; }
        void Connect();
        void Disconnect();
    }
}