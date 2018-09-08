using Microsoft.Extensions.Logging;
using TwitchLib.Client;

namespace EvilBot
{
    public interface ILoggerManager
    {
        ILogger<TwitchClient> Logger { get; set; }
    }
}