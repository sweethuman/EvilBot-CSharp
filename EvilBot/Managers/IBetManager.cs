using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.Resources.Enums;

namespace EvilBot.Managers
{
	public interface IBetManager
	{
		string BetName { get; }
		bool BetActive { get; }
		bool BetLocked { get; }
		List<(string userId, int points)> Winners { get; }
		int LatestPrize { get; }
		bool CreateBet(string betName);
		BetState CancelBet();
		Task<BetState> EndBetAsync(int option);
		Task<BetState> MakeVoteAsync(int points, int option, string userId);
		BetState UndoVote(string userId);
		BetState IsVotePresent(string userId);
		void BetOn();
		void BetOff();
		double PotentialWin(string userId);
		(int points, int option) GetUserVote(string userId);
		(int voters, int poolSum) GetOptionAttributes(int option);
		bool IsOptionValid(int option);
	}
}
