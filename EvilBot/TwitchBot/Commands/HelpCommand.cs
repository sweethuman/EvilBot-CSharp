using System.Threading.Tasks;
using EvilBot.TwitchBot.Commands.Interfaces;
using EvilBot.Utilities;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class HelpCommand : ITwitchCommand
	{

		private string CommandsString { get; }
		private string CommandsModString { get; }

		public HelpCommand(string commandsString, string commandsModString)
		{
			CommandsString = commandsString;
			CommandsModString = commandsModString;
		}

		public bool NeedMod { get; } = false;

		public Task<string> ProcessorAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult(CommandHelpers.ChangeOutputIfMod(e.Command.ChatMessage.UserType, CommandsString, CommandsModString));

	}
}
