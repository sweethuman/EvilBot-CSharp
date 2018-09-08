using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using TwitchLib.Api;
using TwitchLib.Api.Models.v5.Users;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace EvilBot
{
    internal class TwitchChatBot
    {
        private static TwitchAPI api;
        private readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        private TwitchClient client;

        private Timer addPointsTimer;
        private Timer addLurkerPointsTimer;
        private Timer messageRepeater;

        private readonly LoggerManager loggerManager = new LoggerManager();

        public static SqliteDataAccess SqliteDataAccess { get; } = new SqliteDataAccess();

        internal void Connect()
        {
            Console.WriteLine("Connecting");
            client = new TwitchClient(logger: loggerManager.Logger);
            client.Initialize(credentials, TwitchInfo.ChannelName);

            ApiInitialize();
            ChatThrottlerInitialize();
            EventIntializer();

            client.Connect();

            TimerInitializer();
        }

        private void TimerInitializer()
        {
            addPointsTimer = new Timer(1000 * 60 * 1);
            addPointsTimer.Elapsed += AddPointsTimer_ElapsedAsync;
            addPointsTimer.Start();

            addLurkerPointsTimer = new Timer(1000 * 60 * 10);
            addLurkerPointsTimer.Elapsed += AddLurkerPointsTimer_ElapsedAsync;
            addLurkerPointsTimer.Start();

            messageRepeater = new Timer(1000 * 60 * 5);
            messageRepeater.Elapsed += MessageRepeater_Elapsed;
            messageRepeater.Start();
        }

        private void EventIntializer()
        {
            client.OnLog += Client_OnLog;
            client.OnMessageSent += Client_OnMessageSent;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnMessageReceived += Client_OnMessageReceived;
            //client.OnWhisperReceived += Client_OnWhisperReceived;
        }

        private async void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = await api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).ConfigureAwait(false);
            List<Task> addPointsTasks = new List<Task>();
            List<Task<string>> userIdTasks = new List<Task<string>>();
            for (int i = 0; i < chatusers.Count; i++)
            {
                userIdTasks.Add(GetUserIdAsync(chatusers[i].Username));
            }
            var userIDList = await Task.WhenAll(userIdTasks).ConfigureAwait(false);
            for (int i = 1; i < chatusers.Count; i++)
            {
                addPointsTasks.Add(SqliteDataAccess.AddPointToUserID(userIDList[i]));
            }
            await Task.WhenAll(addPointsTasks).ConfigureAwait(false);
            Log.Debug("Database updated! Lurkers present: {Lurkers}", chatusers.Count);
        }

        private async void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e)
        {
            List<string> temporaryTalkers = PointCounter.ClearTalkerPoints();
            for (int i = 0; i < temporaryTalkers.Count; i++)
            {
                await SqliteDataAccess.AddPointToUserID(temporaryTalkers[i]).ConfigureAwait(false);
            }
            Log.Debug("Database updated! Talkers present: {Talkers}", temporaryTalkers.Count);
        }

        private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, "/me Incearca !points si vezi cat de activ ai fost");
        }

        private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            Console.WriteLine($" - - - sent channel: {e.SentMessage.Channel}");
        }

        private static void ApiInitialize()
        {
            api = new TwitchAPI();
            api.Settings.ClientId = TwitchInfo.ClientID;
            api.Settings.AccessToken = TwitchInfo.BotToken;
        }

        private void ChatThrottlerInitialize()
        {
            client.ChatThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 85, TimeSpan.FromSeconds(30));
            client.WhisperThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 85, TimeSpan.FromSeconds(30));
            client.ChatThrottler.StartQueue();
            client.WhisperThrottler.StartQueue();
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting");
        }

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            Console.WriteLine($" - - - arg channel: {e.Command.ChatMessage.Channel}!");
            switch (e.Command.CommandText)
            {
                case "viewers":
                    Log.Debug("{Username} asked for Viewers!", e.Command.ChatMessage.Username);
                    client.SendMessage(TwitchInfo.ChannelName, "/me Incearca !points si vezi cat de activ ai fost");
                    break;

                case "points":
                    if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                    {
                        string points = await SqliteDataAccess.RetrievePointsAsync(e.Command.ChatMessage.UserId).ConfigureAwait(false);
                        if (points != null)
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ChatMessage.DisplayName} You have: {points} points! Be active to gain more!\n\r");
                        }
                        else
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ChatMessage.DisplayName} You aren't yet in the database, hang on a little bit more and you'll be added at the next check!\n\r");
                        }
                    }
                    else
                    {
                        string points = await SqliteDataAccess.RetrievePointsAsync(await GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false)).ConfigureAwait(false);
                        if (points != null)
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ArgumentsAsString.TrimStart('@')} has: {points} points!");
                        }
                        else
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ArgumentsAsString.TrimStart('@')} isn't yet in the database!");
                        }
                    }
                    Log.Debug("{DisplayName} asked for points!", e.Command.ChatMessage.DisplayName);
                    break;

                default:
                    Console.WriteLine($" - - {e.Command.ChatMessage.DisplayName} used an unknow command!(!{e.Command.CommandText})");
                    break;
            }
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"Error!!! {e.Error}");
            Log.Error("Error!!! {Error}", e.Error);
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private async void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (!e.ChatMessage.Message.StartsWith("!"))
            {
                PointCounter.AddMessagePoint(e.ChatMessage.UserId);
                if (e.ChatMessage.Message.StartsWith("hi", StringComparison.InvariantCultureIgnoreCase))
                {
                    client.SendMessage(e.ChatMessage.Channel, $"Hey there @{e.ChatMessage.DisplayName}");
                }
                else
                if (e.ChatMessage.Message.StartsWith("salut", StringComparison.InvariantCultureIgnoreCase))
                {
                    client.SendMessage(e.ChatMessage.Channel, $"Salut @{e.ChatMessage.DisplayName}!");
                }
                else
                if (e.ChatMessage.Message.StartsWith("buna", StringComparison.InvariantCultureIgnoreCase))
                {
                    client.SendMessage(e.ChatMessage.Channel, $"Buna @{e.ChatMessage.DisplayName}!");
                }
                else
                if (e.ChatMessage.Message.StartsWith("ceau", StringComparison.InvariantCultureIgnoreCase))
                {
                    client.SendMessage(e.ChatMessage.Channel, $"Ceau @{e.ChatMessage.DisplayName}!");
                }
                else
                if (e.ChatMessage.Message.StartsWith("wot", StringComparison.InvariantCultureIgnoreCase))
                {
                    client.SendMessage(e.ChatMessage.Channel, (await GetUptimeAsync().ConfigureAwait(false))?.ToString() ?? "Offline");
                }
            }
        }

        //private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        //{
        //    client.SendWhisper(e.WhisperMessage.Username, $"You said: {e.WhisperMessage.Message}");
        //}

        private async Task<TimeSpan?> GetUptimeAsync()
        {
            string userId = await GetUserIdAsync(TwitchInfo.ChannelName).ConfigureAwait(false);
            if (userId == null)
            {
                return null;
            }
            return api.Streams.v5.GetUptimeAsync(userId).Result;
        }

        private static async Task<string> GetUserIdAsync(string username)
        {
            Log.Debug("AskedForID for {Username}", username);
            User[] userList = (await api.Users.v5.GetUserByNameAsync(username).ConfigureAwait(false)).Matches;
            if (username == null || userList.Length == 0)
            {
                return null;
            }
            return userList[0].Id;
        }
    }
}