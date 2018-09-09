using Microsoft.Extensions.Logging;
using TwitchLib.Client;

namespace EvilBot
{
    public interface ILoggerManager
    {
        ILogger<TwitchClient> ClientLogger { get; set; }
        ILoggerFactory APILoggerFactory { get; set; }
    }
}