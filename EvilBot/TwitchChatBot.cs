﻿using System;
using System.Collections.Generic;
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
        //Timer _timer;

        internal void Connect()
        {
            Console.WriteLine("Connecting");

            client = new TwitchClient();
            client.Initialize(credentials, TwitchInfo.ChannelName);

            client.ChatThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 15, TimeSpan.FromSeconds(30));
            client.WhisperThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 15, TimeSpan.FromSeconds(30));
            client.ChatThrottler.StartQueue();
            client.WhisperThrottler.StartQueue();

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;
            client.Connect();

            api = new TwitchAPI();
            api.Settings.ClientId = TwitchInfo.ClientID;
            api.Settings.AccessToken = TwitchInfo.BotToken;

            Console.WriteLine(SqliteDataAccess.RetrievePoints("nightbot"));
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).Result;
            SqliteDataAccess.AddPointToUsername(chatusers);

            //_timer = new Timer(5000);
            //_timer.Elapsed += _timer_Elapsed;
            //_timer.Enabled = true;
        }

        internal void Disconnect()
        {
            Console.WriteLine("Disconnecting");
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
                        string points = SqliteDataAccess.RetrievePoints(e.Command.ChatMessage.Username);
                        if (points != null)
                            client.SendMessage(TwitchInfo.ChannelName, $"{e.Command.ChatMessage.DisplayName} You have: {points} points! Be active to gain more!");
                        else
                            client.SendMessage(TwitchInfo.ChannelName, $"{e.Command.ChatMessage.DisplayName} You aren't yet in the database, hang on a little bit more and you'll be added at the next check!");
                    }
                    else
                    {
                        string points = SqliteDataAccess.RetrievePoints(e.Command.ArgumentsAsString.TrimStart('@').ToLower());
                        if (points != null)
                            client.SendMessage(TwitchInfo.ChannelName, $"{e.Command.ArgumentsAsString.TrimStart('@')} has: {points} points!");
                        else
                            client.SendMessage(TwitchInfo.ChannelName, $"{e.Command.ArgumentsAsString.TrimStart('@')} isn't yet in the database!");
                    }
                    break;

                default:
                    Console.WriteLine($" - - {e.Command.ChatMessage.DisplayName} used an unknow command!(!{e.Command.CommandText})");
                    break;
            }
        }

        /*private void _timer_Elapsed(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine("Timer Elapsed!");
        }*/

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"Error!!! {e.Error}");
        }

        private void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine(e.Data);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (e.ChatMessage.Message.StartsWith("hi", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"Hey there @{e.ChatMessage.DisplayName}");
            }
            else

            if (e.ChatMessage.Message.StartsWith("wot", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(TwitchInfo.ChannelName, GetUptime()?.ToString() ?? "Offline");
            }
        }

        private void Client_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            client.SendWhisper(e.WhisperMessage.Username, $"You said: {e.WhisperMessage.Message}");
        }

        private TimeSpan? GetUptime()
        {
            string userId = GetUserId(TwitchInfo.ChannelName);
            if (userId == null)
            {
                return null;
            }
            return api.Streams.v5.GetUptimeAsync(userId).Result;
        }

        private string GetUserId(string username)
        {
            User[] userList = api.Users.v5.GetUserByNameAsync(username).Result.Matches;
            if (username == null || userList.Length == 0)
            {
                return null;
            }
            return userList[0].Id;
        }

        private string ViewerList()
        {
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).Result;
            Console.WriteLine($"Called ViewerList Command!");
            var viewers = new System.Text.StringBuilder();
            for (int i = 0; i < chatusers.Count; i++)
            {
                viewers.AppendLine($"@{chatusers[i].Username}");
            }
            return viewers.ToString();
        }
    }
}