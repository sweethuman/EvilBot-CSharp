using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using Serilog;

namespace EvilBot.Managers
{
	public class BetManager : IBetManager
	{
		private readonly IDataAccess _dataAccess;
		private Dictionary<string, (int points, int option)> _voteData;

		public BetManager(IDataAccess dataAccess)
		{
			_dataAccess = dataAccess;
		}

		public string BetName { get; private set; }
		public bool BetActive { get; private set; }
		public bool BetLocked { get; private set; }

		public int LatestPrize { get; private set; }
		public List<(string userId, int points)> Winners { get; private set; }

		public bool CreateBet(string betName)
		{
			if (BetActive) return false;
			BetName = betName;
			BetActive = true;
			BetLocked = false;
			_voteData = new Dictionary<string, (int points, int option)>();
			Log.Debug("Betting Started");
			return true;
		}

		public BetState CancelBet()
		{
			if (!BetActive) return BetState.BetNotActive;
			BetActive = false;
			_voteData = null;
			Log.Debug("BetCanceled");
			return BetState.ActionSucceeded;
		}

		public async Task<BetState> EndBetAsync(int option)
		{
			if (!BetActive) return BetState.BetNotActive;
			if (!IsOptionValid(option)) return BetState.OptionInvalid;
			Log.Debug("Ending Betting, option: {0}", option);
			var prizePool = GetOptionAttributes(option == 1 ? 2 : 1).poolSum * 90 / 100;
			LatestPrize = prizePool;
			var currentPool = GetOptionAttributes(option).poolSum;
			BetActive = false;
			var pointsTasks = new List<Task>();
			var winners = new List<(string userId, int points)>();
			foreach (var vote in _voteData)
				if (vote.Value.option == option)
				{
					var prize = (int) (vote.Value.points / (double)currentPool * prizePool);
					pointsTasks.Add(_dataAccess.ModifierUserIdAsync(vote.Key, prize));
					winners.Add((vote.Key, prize));
				}
				else
				{
					pointsTasks.Add(_dataAccess.ModifierUserIdAsync(vote.Key, -1 * vote.Value.points));
				}

			await Task.WhenAll(pointsTasks).ConfigureAwait(false);
			winners.Sort((x, y) => x.points - y.points);
			Winners = winners;
			return BetState.ActionSucceeded;
		}

		public async Task<BetState> MakeVoteAsync(int points, int option, string userId)
		{
			if (!BetActive) return BetState.BetNotActive;
			if (BetLocked) return BetState.BetLocked;
			if (points <= 10) return BetState.ActionFailed;
			if (string.IsNullOrEmpty(userId)) return BetState.ActionError;
			if (!IsOptionValid(option)) return BetState.OptionInvalid;
			Log.Debug("Bet made for {userId} of {points} to option {option}", userId, points, option);
			var betValue = 0;
			if (_voteData.ContainsKey(userId)) betValue += _voteData[userId].points;
			betValue += points;
			var user = await _dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, userId)
				.ConfigureAwait(false);
			if (!int.TryParse(user.Points, out var databasePoints))
			{
				Log.Error("Conversion to points : {points}", user.Points);
				return BetState.ActionError;
			}

			if (betValue > databasePoints) return BetState.NotEnoughPoints;
			_voteData[userId] = (betValue, option);
			return BetState.ActionSucceeded;
		}

		public BetState UndoVote(string userId)
		{
			if (!BetActive) return BetState.BetNotActive;
			if (BetLocked) return BetState.BetLocked;
			if (!_voteData.Remove(userId))
			{
				Log.Debug("Bet Couldn't be Undone for {userId}", userId);
				return BetState.ActionFailed;
			}

			Log.Debug("Bet Undone for {userId}", userId);
			return BetState.ActionSucceeded;
		}

		public BetState IsVotePresent(string userId)
		{
			if (!BetActive) return BetState.BetNotActive;
			return _voteData.ContainsKey(userId) ? BetState.ActionSucceeded : BetState.ActionFailed;
		}

		public void BetOn()
		{
			BetLocked = false;
		}

		public void BetOff()
		{
			BetLocked = true;
		}

		public double PotentialWin(string userId)
		{
			if (!_voteData.ContainsKey(userId)) return 0;
			var option = _voteData[userId].option;
			var prizePool = GetOptionAttributes(option == 1 ? 2 : 1).poolSum * 90 / 100;
			var currentPool = GetOptionAttributes(option).poolSum;
			return _voteData[userId].points / (double) currentPool * prizePool;
		}

		public (int points, int option) GetUserVote(string userId)
		{
			if (!BetActive) return (0, 0);
			if (_voteData.TryGetValue(userId, out var vote))
				return vote;
			return (0, 0);
		}

		public (int voters, int poolSum) GetOptionAttributes(int option)
		{
			if (!BetActive) return (0, 0);
			if (!IsOptionValid(option)) return (0, 0);
			int poolSum = 0, voters = 0;
			foreach (var vote in _voteData)
			{
				if (vote.Value.option != option) continue;
				poolSum += vote.Value.points;
				++voters;
			}

			return (voters, poolSum);
		}

		public bool IsOptionValid(int option)
		{
			return option == 1 || option == 2;
		}
	}
}
