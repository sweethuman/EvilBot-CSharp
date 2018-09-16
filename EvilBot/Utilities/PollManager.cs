using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EvilBot
{
    public class PollManager : IPollManager
    {
        public List<string> PollItems { get; set; } = null;
        public List<double> PollVotes { get; private set; } = null;
        public bool PollActive { get; private set; } = false;

        private List<double> InfluencePoints { get; set; } = new List<double> { 1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8 };
        private List<string> UsersWhoVoted;
        private IDataAccess _dataAccess;

        public PollManager(IDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public string PollCreate(List<string> optionsList)
        {
            //t: in case given string is empty or null to not add it to the options
            Log.Debug("PollStared");
            PollItems = optionsList;
            UsersWhoVoted = new List<string>();
            PollVotes = new List<double>();
            for (int i = 0; i < PollItems.Count; i++)
            {
                PollVotes.Add(0);
            }
            PollActive = true;
            //t: use string builder for improved performance
            string message = $"Poll Created! Poll Options ";
            for (int i = 0; i < PollItems.Count; i++)
            {
                message = $"{message} // {i + 1}:{PollItems[i]}";
            }
            return message;
        }

        public string PollEnd()
        {
            PollActive = false;
            int winner = 0;
            for (int i = 1; i < PollItems.Count; i++)
            {
                if (PollVotes[winner] < PollVotes[i])
                {
                    winner = i;
                }
            }
            string message = $"A Castigat || {PollItems[winner]} || cu {PollVotes[winner]} puncte";
            PollItems = null;
            PollVotes = null;
            UsersWhoVoted = null;
            Log.Debug("Poll Ended");
            return message;
        }

        public string PollStats()
        {
            string message = "Poll Stats:";
            for (int i = 0; i < PollItems.Count; i++)
            {
                message = $"{message} // {PollItems[i]}: {PollVotes[i]}";
            }
            return message;
        }

        //NOTE make sure it doesn't get negative or 0 numbers at optionNumber
        public async Task PollAddVote(string userID, int optionNumber)
        {
            if (userID != null && !UsersWhoVoted.Contains(userID) && optionNumber <= PollItems.Count && optionNumber >= 1)
            {
                string userRank = await _dataAccess.RetrieveRowAsync(userID, Enums.DatabaseRow.Rank).ConfigureAwait(false) ?? "0";

                if (int.TryParse(userRank, out int rank) && rank < InfluencePoints.Count)
                {
                    PollVotes[optionNumber - 1] += InfluencePoints[rank];
                    UsersWhoVoted.Add(userID);
                    Log.Debug("{UserID} voted", userID);
                }
                else
                {
                    Log.Warning("Rank was not a parsable: {Rank} {Class}", userRank, this);
                }
            }
        }
    }
}