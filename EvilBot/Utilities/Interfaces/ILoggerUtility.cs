using Microsoft.Extensions.Logging;
using TwitchLib.Client;

namespace EvilBot.Utilities.Interfaces
{
	public interface ILoggerUtility
	{
		ILogger<TwitchClient> ClientLogger { get; set; }
		ILoggerFactory ApiLoggerFactory { get; set; }
	}
}