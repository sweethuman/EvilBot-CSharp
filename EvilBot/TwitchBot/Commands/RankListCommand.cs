using System.Threading.Tasks;
using EvilBot.Managers.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class RankListCommand : ITwitchCommand
	{
		private readonly IRankManager _rankManager;

		public RankListCommand(IRankManager rankManager)
		{
			_rankManager = rankManager;
		}

		public bool NeedMod { get; } = false;

		public Task<string> ProcessorAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult($"/me {_rankManager.RankListString}");
	}
}
