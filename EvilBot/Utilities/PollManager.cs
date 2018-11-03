using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures.Database;
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
		public List<string> PollItems { get; set; }
		public List<double> PollVotes { get; private set; }

		public bool PollActive { get; private set; }

		/*NOTE this is probably bad design because it directly returns strings and can have multpiple points of failure
		 and the return type and data is not consistent for this or the way data is processed, it just seems awful*/
		public string PollCreate(List<string> optionsList)
		{
			//t: in case given string is empty or null to not add it to the options
			Log.Debug("PollStarting");
			var builder = new StringBuilder();
			PollItems = optionsList;
			_usersWhoVoted = new List<string>();
			PollVotes = new List<double>();
			PollActive = true;
			for (var i = 0; i < PollItems.Count; i++) PollVotes.Add(0);
			builder.Append("Poll Creat! Optiuni: ");
			for (var i = 0; i < PollItems.Count; i++) builder.AppendFormat(" //{0}:{1}", i + 1, PollItems[i]);
			Log.Debug("PollStarted");
			return builder.ToString();
		}

		public string PollEnd()
		{
			if (!PollActive) return StandardMessages.PollNotActiveText;
			PollActive = false;
			var winner = 0;
			for (var i = 1; i < PollItems.Count; i++)
				if (PollVotes[winner] < PollVotes[i])
					winner = i;
			var message = $"A Castigat || {PollItems[winner]} || cu {PollVotes[winner]} puncte";
			PollItems = null;
			PollVotes = null;
			_usersWhoVoted = null;
			Log.Debug("Poll Ended");
			return message;
		}

		public string PollStats()
		{
			if (!PollActive) return StandardMessages.PollNotActiveText;
			var builder = new StringBuilder();
			builder.Append("Statistici :");
			for (var i = 0; i < PollItems.Count; i++) builder.AppendFormat(" //{0}:{1}", PollItems[i], PollVotes[i]);
			return builder.ToString();
		}

		public async Task<Enums.PollAddVoteFinishState> PollAddVote(string userId, int votedNumber)
		{
			if (!PollActive) return Enums.PollAddVoteFinishState.PollNotActive;
			if (userId == null || _usersWhoVoted.Contains(userId) || votedNumber > PollItems.Count ||
			    votedNumber < 1) return Enums.PollAddVoteFinishState.VoteFailed;

			var user = await _dataAccess.RetrieveUserFromTable(Enums.DatabaseTables.UserPoints, userId) ??
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