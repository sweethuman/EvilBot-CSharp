﻿using Serilog;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace EvilBot
{
    public class PollManager : IPollManager
    {
        public List<string> PollItems { get; set; } = null;
        public List<double> PollVotes { get; private set; } = null;
        public bool PollActive { get; private set; } = false;

        private List<double> InfluencePoints { get; } = new List<double> { 1, 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7, 1.8 };
        private List<string> UsersWhoVoted;
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
            UsersWhoVoted = new List<string>();
            PollVotes = new List<double>();
            PollActive = true;
            for (var i = 0; i < PollItems.Count; i++)
            {
                PollVotes.Add(0);
            }
            builder.Append("Poll Created! Poll Options ");
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
            UsersWhoVoted = null;
            Log.Debug("Poll Ended");
            return message;
        }

        public string PollStats()
        {
            var builder = new StringBuilder();
            builder.Append("Poll Stats:");
            for (var i = 0; i < PollItems.Count; i++)
            {
                builder.AppendFormat(" //{0}:{1}", PollItems[i], PollVotes[i]);
            }
            return builder.ToString();
        }

        //NOTE make sure it doesn't get negative or 0 numbers at optionNumber
        public async Task PollAddVote(string userId, int optionNumber)
        {
            if (userId != null && !UsersWhoVoted.Contains(userId) && optionNumber <= PollItems.Count && optionNumber >= 1)
            {
                var userRank = await _dataAccess.RetrieveRowAsync(userId, Enums.DatabaseRow.Rank).ConfigureAwait(false) ?? "0";

                if (int.TryParse(userRank, out int rank) && rank < InfluencePoints.Count)
                {
                    PollVotes[optionNumber - 1] += InfluencePoints[rank];
                    UsersWhoVoted.Add(userId);
                    Log.Debug("{UserID} voted", userId);
                }
                else
                {
                    Log.Warning("Rank was not a parsable: {Rank} {Class}", userRank, this);
                }
            }
        }
    }
}