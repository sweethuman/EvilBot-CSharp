using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace EvilBot.Processors.Interfaces
{
    internal interface ICommandProcessor
    {
        Task<string> RankCommandAsync(OnChatCommandReceivedArgs e);

        Task<string> ManageCommandAsync(OnChatCommandReceivedArgs e);

        string PollCreateCommand(OnChatCommandReceivedArgs e);

        Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e);

        string PollStatsCommand(OnChatCommandReceivedArgs e);

        string PollEndCommand(OnChatCommandReceivedArgs e);

        Task<string> FilterCommand(OnChatCommandReceivedArgs e);
    }
}