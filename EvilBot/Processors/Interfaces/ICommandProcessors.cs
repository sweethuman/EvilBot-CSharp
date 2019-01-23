using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace EvilBot.Processors.Interfaces
{
	internal interface ICommandProcessor
	{
		string RankListString { get; }
		Task<string> RankCommandAsync(OnChatCommandReceivedArgs e);

		Task<string> ManageCommandAsync(OnChatCommandReceivedArgs e);

		Task<string> FilterCommandAsync(OnChatCommandReceivedArgs e);

		Task<string> TopCommandAsync(OnChatCommandReceivedArgs e);

		Task<(string usersAnnouncement, string winnerAnnouncement)> GiveawayCommandAsync(OnChatCommandReceivedArgs e);
		Task<string> PollCommandAsync(OnChatCommandReceivedArgs e);
	}
}
