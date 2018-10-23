using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures.Database;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
    public class PollManager : IPollManager
    {
        public List<string> PollItems { get; set; }
        public List<double> PollVotes { get; private set; }
        public bool PollActive { get; private set; }

        private List<double> InfluencePoints { get; } = new List<double> { 1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8 };
        private List<string> _usersWhoVoted;
        private readonly IDataAccess _dataAccess;

        public PollManager(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public string PollCreate(List<string> optionsList)
        {
            //t: in case given string is empty or null to not add it to the options
            Log.Debug("PollStarting");
            var builder = new StringBuilder();
            PollItems = optionsList;
            _usersWhoVoted = new List<string>();
            PollVotes = new List<double>();
            PollActive = true;
            for (var i = 0; i < PollItems.Count; i++)
            {
                PollVotes.Add(0);
            }
            builder.Append("Poll Creat! Optiuni: ");
            for (var i = 0; i < PollItems.Count; i++)
            {
                builder.AppendFormat(" //{0}:{1}", i + 1, PollItems[i]);
            }
            Log.Debug("PollStarted");
            return builder.ToString();
        }

        public string PollEnd()
        {
            PollActive = false;
            var winner = 0;
            for (var i = 1; i < PollItems.Count; i++)
            {
                if (PollVotes[winner] < PollVotes[i])
                {
                    winner = i;
                }
            }
            var message = $"A Castigat || {PollItems[winner]} || cu {PollVotes[winner]} puncte";
            PollItems = null;
            PollVotes = null;
            _usersWhoVoted = null;
            Log.Debug("Poll Ended");
            return message;
        }

        public string PollStats()
        {
            var builder = new StringBuilder();
            builder.Append("Statistici :");
            for (var i = 0; i < PollItems.Count; i++)
            {
                builder.AppendFormat(" //{0}:{1}", PollItems[i], PollVotes[i]);
            }
            return builder.ToString();
        }

        public async Task<bool> PollAddVote(string userId, int votedNumber)
        {
            if (userId == null || _usersWhoVoted.Contains(userId) || votedNumber > PollItems.Count ||
                votedNumber < 1) return false;
            
            var user = await _dataAccess.RetrieveUserFromTable(Enums.DatabaseTables.UserPoints, userId) ?? new DatabaseUser{UserID = userId, Rank = "0"};
            if (int.TryParse(user.Rank, out var rank) && rank < InfluencePoints.Count)
            {
                PollVotes[votedNumber - 1] += InfluencePoints[rank];
                _usersWhoVoted.Add(userId);
                Log.Debug("{UserID} voted", userId);
                return true;
            }

            Log.Warning("Rank was not a parsable: {Rank} {Class}", user.Rank, this);

            return false;
        }
    }
}