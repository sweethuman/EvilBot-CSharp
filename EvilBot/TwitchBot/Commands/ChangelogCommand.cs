using System.Threading.Tasks;
using EvilBot.Resources;
using EvilBot.TwitchBot.Commands.Interfaces;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class ChangelogCommand : ITwitchCommand
	{
		public bool NeedMod { get; } = false;

		public Task<string> ProcessorAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult($"/me {StandardMessages.BotInformation.ChangelogBot}");
	}
}
