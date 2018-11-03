using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.Utilities.Resources;

namespace EvilBot.Utilities.Interfaces
{
    public interface IPollManager
    {
        bool PollActive { get; }
        List<string> PollItems { get; set; }
        List<double> PollVotes { get; }

        Task<Enums.PollAddVoteFinishState> PollAddVote(string userId, int votedNumber);

        string PollCreate(List<string> optionsList);

        string PollEnd();

        string PollStats();
    }
}