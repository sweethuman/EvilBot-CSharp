using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Database;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Utilities.Interfaces;
using EvilBot.Utilities.Resources;
using EvilBot.Utilities.Resources.Interfaces;
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
		public List<string> PollItems { get; private set; }
		public List<double> PollVotes { get; private set; }

		public bool PollActive { get; private set; }
		
		public List<string> PollCreate(List<string> optionsList)
		{
			if (optionsList == null || optionsList.Count < 2 || optionsList.Exists(string.IsNullOrEmpty)) return null;
			Log.Debug("PollStarting");
			PollItems = optionsList;
			_usersWhoVoted = new List<string>();
			PollVotes = new List<double>();
			for (var i = 0; i < PollItems.Count; i++) PollVotes.Add(0);
			PollActive = true;
			Log.Debug("PollStarted");
			return PollItems;
		}

		public IPollItem PollEnd()
		{
			if (!PollActive) return null;
			PollActive = false;
			var winner = 0;
			for (var i = 1; i < PollItems.Count; i++)
				if (PollVotes[winner] < PollVotes[i])
					winner = i;
			var pollItem = new PollItem(0,PollVotes[winner], PollItems[winner]);

			PollItems = null;
			PollVotes = null;
			_usersWhoVoted = null;
			Log.Debug("Poll Ended");
			return pollItem;
		}

		public List<IPollItem> PollStats()
		{
			if (!PollActive) return null;
			return PollItems.Select((t, i) => new PollItem(i,PollVotes[i], t)).ToList<IPollItem>();
		}

		public async Task<Enums.PollAddVoteFinishState> PollAddVoteAsync(string userId, int votedNumber)
		{
			if (!PollActive) return Enums.PollAddVoteFinishState.PollNotActive;
			if (userId == null || _usersWhoVoted.Contains(userId)) return Enums.PollAddVoteFinishState.VoteFailed;
			if (votedNumber > PollItems.Count || votedNumber < 1) return Enums.PollAddVoteFinishState.OptionOutOfRange;

			var user = await _dataAccess.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, userId).ConfigureAwait(false) ??
			           new DatabaseUser {UserId = userId, Rank = "0"};
			if (int.TryParse(user.Rank, out var rank) && rank < InfluencePoints.Count)
			{
				PollVotes[votedNumber - 1] += InfluencePoints[rank];
				_usersWhoVoted.Add(userId);
				Log.Debug("{UserID} voted", userId);
				return Enums.PollAddVoteFinishState.VoteAdded;
			}

			Log.Warning("Rank was not a parsable: {Rank} {Class}", user.Rank, this);

			return Enums.PollAddVoteFinishState.VoteAdded;
		}
	}
}