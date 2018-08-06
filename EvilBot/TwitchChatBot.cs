using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using TwitchLib;
using TwitchLib.Api.Models.v5.Users;
using TwitchLib.Api.Models.v5.Streams;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Api;
using TwitchLib.Api.Enums;

namespace EvilBot
{
    internal class TwitchChatBot
    {
        readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        TwitchClient client;
        static TwitchAPI api;
        //Timer _timer;

        internal void Connect()
        {
            Console.WriteLine("Connecting");

            client = new TwitchClient();
            client.Initialize(credentials, TwitchInfo.ChannelName);

            /*client.ChatThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 15, TimeSpan.FromSeconds(30));
            client.WhisperThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 15, TimeSpan.FromSeconds(30));*/

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;


            client.Connect();

            api = new TwitchAPI();
            api.Settings.ClientId = TwitchInfo.ClientID;
            api.Settings.AccessToken = TwitchInfo.BotToken;

            Console.WriteLine(SqliteDataAccess.RetrievePoints("icicicicicc"));
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).Result;
            SqliteDataAccess.AddPointToUsername(chatusers);

            //_timer = new Timer(5000);
            //_timer.Elapsed += _timer_Elapsed;
            //_timer.Enabled = true;
        }

        private void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            switch (e.Command.CommandText)
            {
                case "viewers":
                    client.SendMessage(TwitchInfo.ChannelName, $"Viewer list is: {ViewerList()}");
                    break;
                case "points":
                    if (e.Command.ArgumentsAsString == "")
                    {
                        int points = SqliteDataAccess.RetrievePoints(e.Command.ChatMessage.Username);
                        if (points >= 0)
                            client.SendMessage(TwitchInfo.ChannelName, $"You have: {points} points! Be active to gain more!");
                        else
                            client.SendMessage(TwitchInfo.ChannelName, "You aren't yet in the database, hang on a little bit more and you'll be added at the next check!");
                    }
                    else
                    {
                        int points = SqliteDataAccess.RetrievePoints(e.Command.ArgumentsAsString.TrimStart('@').ToLower());
                        if (points >= 0)
                            client.SendMessage(TwitchInfo.ChannelName, $"{e.Command.ArgumentsAsString} has: {points} points!");
                        else
                            client.SendMessage(TwitchInfo.ChannelName, $"{e.Command.ArgumentsAsString} isn't yet in the database!");
                    }
                    break;
                default:
                    Console.WriteLine($" - - {e.Command.ChatMessage.DisplayName} used an unknow command!(!{e.Command.CommandText})");
                    break;

            }
        }

        private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer Elapsed!");
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            client.SendWhisper(e.WhisperMessage.Username, $"You said: {e.WhisperMessage.Message}");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if(e.ChatMessage.Message.StartsWith("hi", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"Hey there @{e.ChatMessage.DisplayName}");
            }
            else 
            
            if(e.ChatMessage.Message.StartsWith("wot", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(TwitchInfo.ChannelName ,GetUptime()?.ToString() ?? "Offline");
            }
        }

        private string ViewerList()
        {
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).Result;
            Console.WriteLine($"Called ViewerList Command!");
            var viewers = new System.Text.StringBuilder();
            for(int i = 0; i<chatusers.Count; i++)
            {
                viewers.AppendLine($"@{chatusers[i].Username}");
            }
            return viewers.ToString();
        }

        TimeSpan? GetUptime()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);
            if(userId == null)  
            {
                return null;
            }
            return api.Streams.v5.GetUptimeAsync(userId).Result;
        }

        string GetUserId(string username)
        {
            User[] userList = api.Users.v5.GetUserByNameAsync(username).Result.Matches;
            if(username == null || userList.Length == 0)
            {
                return null;
            }
            return userList[0].Id;
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"Error!!! {e.Error}");
        }

        internal void Disconnect()
        {   
            Console.WriteLine("Disconnecting");
        }
    }
}