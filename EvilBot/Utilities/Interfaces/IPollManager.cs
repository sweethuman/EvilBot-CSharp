using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvilBot
{
    public interface IPollManager
    {
        bool PollActive { get; }
        List<string> PollItems { get; set; }
        List<double> PollVotes { get; }

        Task PollAddVote(string userID, int optionNumber);

        string PollCreate(List<string> optionsList);

        string PollEnd();

        string PollStats();
    }
}