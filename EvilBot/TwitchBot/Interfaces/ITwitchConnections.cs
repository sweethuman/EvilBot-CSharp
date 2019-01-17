using TwitchLib.Api.Interfaces;
using TwitchLib.Client.Interfaces;

namespace EvilBot.TwitchBot.Interfaces
{
	/// <summary>
	///     Only used for getting TwitchConnections from a connections class.
	/// </summary>
	public interface ITwitchConnections
	{
		ITwitchAPI Api { get; }
		ITwitchClient Client { get; }

		void Connect();

		void Disconnect();
		void SendErrorMessage(string message);
	}
}