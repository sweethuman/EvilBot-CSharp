using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
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

        internal void Connect()
        {
            Console.WriteLine("Connecting");

            client = new TwitchClient();
            client.Initialize(credentials, TwitchInfo.ChannelName);

            /*client.ChatThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 15, TimeSpan.FromSeconds(30));
            client.WhisperThrottler = new TwitchLib.Client.Services.MessageThrottler(client, 15, TimeSpan.FromSeconds(30));*/

            client.OnLog += Client_OnLog;
            client.OnConnectionError += Client_OnConnectionError;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnWhisperReceived += Client_OnWhisperReceived;

            client.Connect();

            api = new TwitchAPI();
            api.Settings.ClientId = TwitchInfo.ClientID;
            api.Settings.AccessToken = TwitchInfo.BotToken;

            //List<string> usernames = SqliteDataAccess.LoadUsernames();
            //Console.WriteLine(usernames[0]);
            List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> chatusers = api.Undocumented.GetChattersAsync(TwitchInfo.ChannelName).Result;
            SqliteDataAccess.AddPointToUsername(chatusers);

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

            if(e.ChatMessage.Message.StartsWith("!viewers", StringComparison.InvariantCultureIgnoreCase))
            {
                client.SendMessage(TwitchInfo.ChannelName, $"Viewer list is: {ViewerList()}");
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