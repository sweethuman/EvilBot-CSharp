using Serilog;
using System;
using System.Collections.Generic;
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

        public string GetRank(string pointsString)
        {
            int points;
            int place = 0;
            if (int.TryParse(pointsString, out points))
            {
                for (int i = 0; i < ranks.Count - 1; i++)
                {
                    if (points < ranks[i + 1].Item2)
                    {
                        break;
                    }
                    place = i + 1;
                }
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

        public async void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = await _twitchChatBot.Api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).ConfigureAwait(false);
            List<Task> addPointsTasks = new List<Task>();
            List<Task<string>> userIdTasks = new List<Task<string>>();
            for (int i = 0; i < chatusers.Count; i++)
            {
                userIdTasks.Add(GetUserIdAsync(chatusers[i].Username));
            }
            var userIDList = await Task.WhenAll(userIdTasks).ConfigureAwait(false);
            for (int i = 0; i < chatusers.Count; i++)
            {
                addPointsTasks.Add(_dataAccess.AddPointToUserID(userIDList[i], minutes: 10));
            }
            await Task.WhenAll(addPointsTasks).ConfigureAwait(false);
            Log.Debug("Database updated! Lurkers present: {Lurkers}", chatusers.Count);
        }

        public async void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            List<string> temporaryTalkers = PointCounter.ClearTalkerPoints();
            for (int i = 0; i < temporaryTalkers.Count; i++)
            {
                await _dataAccess.AddPointToUserID(temporaryTalkers[i]).ConfigureAwait(false);
            }
            Log.Debug("Database updated! Talkers present: {Talkers}", temporaryTalkers.Count);
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

        public async Task<string[]> GetPointsMinutesAsync(string userID)
        {
            if (userID == null)
            {
                return null;
            }

            List<Task<string>> tasks = new List<Task<string>>
            {
                _dataAccess.RetrieveRowAsync(userID),
                _dataAccess.RetrieveRowAsync(userID, true)
            };
            var results = await Task.WhenAll(tasks).ConfigureAwait(false);
            if (results == null || results[0] == null)
            {
                return null;
            }

            return results;
        }
    }
}