using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands.Interfaces
{
	public interface ITwitchCommand
	{
		bool NeedMod { get; }
		Task<string> ProcessorAsync(OnChatCommandReceivedArgs e);
	}
}