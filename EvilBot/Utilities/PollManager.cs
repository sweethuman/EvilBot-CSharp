using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Database;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
	public class PollManager : IPollManager
	{
		private readonly IDataAccess _dataAccess;
		private List<string> _usersWhoVoted;

		public PollManager(IDataAccess dataAccess)
		{
			_dataAccess = dataAccess;
		}

		private List<double> InfluencePoints { get; } = new List<double> {1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8};

		public List<IPollItem> PollItems { get; private set; }

		public bool PollActive { get; private set; }

		public bool PollCreate(List<string> optionsList)
		{
			if (optionsList == null || optionsList.Count < 2 || optionsList.Exists(string.IsNullOrEmpty)) return false;
			Log.Debug("PollStarting");
			PollItems = optionsList.Select((t, i) => new PollItem(i, 0, t)).ToList<IPollItem>();
			_usersWhoVoted = new List<string>();
			PollActive = true;
			Log.Debug("PollStarted");
			return true;
		}

		public IPollItem PollEnd()
		{
			if (!PollActive) return null;
			PollActive = false;
			var winner = 0;
			for (var i = 1; i < PollItems.Count; i++)
				if (PollItems[winner].Points < PollItems[i].Points)
					winner = i;
			var pollItem = PollItems[winner];
			PollItems = null;
			_usersWhoVoted = null;
			Log.Debug("Poll Ended");
			return pollItem;
		}

		public List<IPollItem> PollStats()
		{
			return !PollActive ? null : PollItems;
		}

		public async Task<Enums.PollAddVoteFinishState> PollAddVoteAsync(string userId, int votedNumber)
		{
			if (!PollActive) return Enums.PollAddVoteFinishState.PollNotActive;
			if (userId == null) return Enums.PollAddVoteFinishState.VoteFailed;
			if (_usersWhoVoted.Contains(userId)) return Enums.PollAddVoteFinishState.AlreadyVoted;
			if (votedNumber > PollItems.Count || votedNumber < 1) return Enums.PollAddVoteFinishState.OptionOutOfRange;

			var user = await _dataAccess.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, userId)
				           .ConfigureAwait(false) ??
			           new DatabaseUser {UserId = userId, Rank = "0"};
			if (int.TryParse(user.Rank, out var rank) && rank < InfluencePoints.Count)
			{
				PollItems[votedNumber - 1].Points += InfluencePoints[rank];
				_usersWhoVoted.Add(userId);
				Log.Debug("{UserID} voted", userId);
				return Enums.PollAddVoteFinishState.VoteAdded;
			}

			Log.Warning("Rank was not a parsable: {Rank} {Class}", user.Rank, this);

			return Enums.PollAddVoteFinishState.VoteFailed;
		}
	}
}