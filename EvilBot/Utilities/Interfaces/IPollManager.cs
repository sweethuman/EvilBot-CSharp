using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Utilities.Resources;

namespace EvilBot.Utilities.Interfaces
{
	public interface IPollManager
	{
		bool PollActive { get; }
		List<string> PollItems { get; }
		List<double> PollVotes { get; }

		Task<Enums.PollAddVoteFinishState> PollAddVote(string userId, int votedNumber);

		List<string> PollCreate(List<string> optionsList);

		IPollItem PollEnd();

		List<IPollItem> PollStats();
	}
}