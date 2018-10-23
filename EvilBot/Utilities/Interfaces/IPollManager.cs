using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvilBot.Utilities.Interfaces
{
    public interface IPollManager
    {
        bool PollActive { get; }
        List<string> PollItems { get; set; }
        List<double> PollVotes { get; }

        Task<bool> PollAddVote(string userId, int votedNumber);

        string PollCreate(List<string> optionsList);

        string PollEnd();

        string PollStats();
    }
}