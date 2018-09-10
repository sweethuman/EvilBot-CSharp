using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using TwitchLib.Api.Models.v5.Users;

namespace EvilBot
{
    internal class DataProcessor : IDataProcessor
    {
        private IDataAccess _dataAccess;
        private ITwitchConnections _twitchChatBot;
        private List<Tuple<string, int>> ranks = new List<Tuple<string, int>>();

        public event EventHandler<RankUpdateEventArgs> RankUpdated;

        protected virtual void OnRankUpdated(string Name, string Rank)
        {
            RankUpdated?.Invoke(this, new RankUpdateEventArgs { Name = Name, Rank = Rank });
        }

        public DataProcessor(IDataAccess dataAccess, ITwitchConnections twitchChatBot)
        {
            _dataAccess = dataAccess;
            _twitchChatBot = twitchChatBot;
            IntializeRanks();
        }

        private void IntializeRanks()
        {
            ranks.Add(new Tuple<string, int>("Fara Rank", 0));
            ranks.Add(new Tuple<string, int>("Rookie", 50));
            ranks.Add(new Tuple<string, int>("Alpha", 500));
            ranks.Add(new Tuple<string, int>("Thug", 2500));
            ranks.Add(new Tuple<string, int>("Sage", 6000));
            ranks.Add(new Tuple<string, int>("Lord", 10000));
            ranks.Add(new Tuple<string, int>("Initiate", 15000));
            ranks.Add(new Tuple<string, int>("Veteran", 22000));
            ranks.Add(new Tuple<string, int>("Emperor", 30000));
        }

        public string GetRankFormatted(string pointsString)
        {
            if (int.TryParse(pointsString, out int points))
            {
                int place = GetRank(points);
                if (place == 0)
                {
                    return $"{ranks[place].Item1} XP: {points}/{ranks[place + 1].Item2}";
                }
                if (place == ranks.Count - 1)
                {
                    return $"{ranks[place].Item1} (Lvl.{place}) XP: {points}";
                }
                return $"{ranks[place].Item1} (Lvl.{place}) XP: {points}/{ranks[place + 1].Item2}";
            }
            else
            {
                Log.Error("pointsString {pointsString} is not a parsable value to int {GetRank}", pointsString, ToString());
                return null;
            }
        }

        private int GetRank(int points)
        {
            int place = 0;
            for (int i = 0; i < ranks.Count - 1; i++)
            {
                if (points < ranks[i + 1].Item2)
                {
                    break;
                }
                place = i + 1;
            }
            return place;
        }

        public async void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = await _twitchChatBot.Api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).ConfigureAwait(false);

            List<Task<string>> userIdTasks = new List<Task<string>>();
            for (int i = 0; i < chatusers.Count; i++)
            {
                userIdTasks.Add(GetUserIdAsync(chatusers[i].Username));
            }
            string[] userIDList = await Task.WhenAll(userIdTasks).ConfigureAwait(false);
            Log.Debug("shit");
            await AddsToUsersAsync(userIDList.ToList(), minutes: 10);
            Log.Debug("Database updated! Lurkers present: {Lurkers}", chatusers.Count);
        }

        public async void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            List<string> temporaryTalkers = PointCounter.ClearTalkerPoints();
            await AddsToUsersAsync(temporaryTalkers);
            Log.Debug("Database updated! Talkers present: {Talkers}", temporaryTalkers.Count);
        }

        private async Task UpdateRankAsync(List<string> userIDList)
        {   //WARNING GetUserAttributesAsync() also gets minutes, wich I don't currently need and it might cause performance issues if volume is large
            List<Task<List<string>>> userAttributesTasks = new List<Task<List<string>>>();
            for (int i = 0; i < userIDList.Count; i++)
            {
                userAttributesTasks.Add(GetUserAttributesAsync(userIDList[i]));
            }
            var userAttributes = (await Task.WhenAll(userAttributesTasks).ConfigureAwait(false)).ToList();
            List<Task<string>> userNameListTasks = new List<Task<string>>();
            List<int> userNameRanks = new List<int>();
            for (int i = 0; i < userAttributes.Count; i++)
            {
                Log.Warning($"{i} shit3");
                if (!int.TryParse(userAttributes[i][0], out int points))
                {
                    Log.Error("Tried to parse string to int: {string} in {ClassSource}", userAttributes[i][1], $"{ToString()}UpdateRankAsync");
                }
                if (!int.TryParse(userAttributes[i][2], out int rank))
                {
                    Log.Error("Tried to parse string to int: {string} in {ClassSource}", userAttributes[i][1], $"{ToString()}UpdateRankAsync");
                }
                int currentRank = GetRank(points);
                if (currentRank != rank)
                {
                    userNameRanks.Add(currentRank);
                    //TODO add tasklist to modify the rank in the database
                    userNameListTasks.Add(GetUsernameAsync(userIDList[i]));
                }
            }

            var userNameList = (await Task.WhenAll(userNameListTasks).ConfigureAwait(false)).ToList();
            for (int i = 0; i < userNameList.Count; i++)
            {
                OnRankUpdated(userNameList[i], $"{ranks[userNameRanks[i]].Item1} (Lvl. {userNameRanks[i]})");
            }
        }

        //TODO make it support a single string too
        public async Task AddsToUsersAsync(List<string> userIDList, int points = 1, int minutes = 0)
        {
            if (userIDList.Count != 0)
            {
                List<Task> addPointsTasks = new List<Task>();
                for (int dnd = 0; dnd < userIDList.Count; dnd++)
                {
                    Log.Warning($"{dnd} shit2");
                    addPointsTasks.Add(_dataAccess.ModifierUserIDAsync(userIDList[dnd], points: points, minutes: minutes));
                }
                await Task.WhenAll(addPointsTasks).ConfigureAwait(false);
                await UpdateRankAsync(userIDList).ConfigureAwait(false);
            }
        }

        public async Task<TimeSpan?> GetUptimeAsync()
        {
            string userId = await GetUserIdAsync(TwitchInfo.ChannelName).ConfigureAwait(false);
            if (userId == null)
            {
                return null;
            }
            return _twitchChatBot.Api.Streams.v5.GetUptimeAsync(userId).Result;
        }

        public async Task<string> GetUserIdAsync(string username)
        {
            Log.Debug("AskedForID for {Username}", username);
            User[] userList = (await _twitchChatBot.Api.Users.v5.GetUserByNameAsync(username).ConfigureAwait(false)).Matches;
            if (username == null || userList.Length == 0)
            {
                return null;
            }
            return userList[0].Id;
        }

        public async Task<string> GetUsernameAsync(string userID)
        {
            Log.Debug("AskedForUsername for {Username}", userID);
            User user = await _twitchChatBot.Api.Users.v5.GetUserByIDAsync(userID).ConfigureAwait(false);
            if (userID == null || user == null)
            {
                return null;
            }
            return user.Name;
        }

        public async Task<List<string>> GetUserAttributesAsync(string userID)
        {
            if (userID == null)
            {
                return null;
            }

            List<Task<string>> tasks = new List<Task<string>>
            {
                _dataAccess.RetrieveRowAsync(userID),
                _dataAccess.RetrieveRowAsync(userID, Enums.DatabaseRow.Minutes),
                _dataAccess.RetrieveRowAsync(userID, Enums.DatabaseRow.Rank)
            };
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            if (results == null || results[0] == null)
            {
                return null;
            }

            return results.ToList();
        }
    }
}