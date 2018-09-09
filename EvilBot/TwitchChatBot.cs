using Autofac;
using Serilog;
using System;
using System.Timers;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace EvilBot
{
    internal class TwitchChatBot : ITwitchChatBot, ITwitchConnections
    {
        private static TwitchAPI api;
        private readonly ConnectionCredentials credentials = new ConnectionCredentials(TwitchInfo.BotUsername, TwitchInfo.BotToken);
        private static TwitchClient client;

        public TwitchClient Client
        {
            get
            {
                return client;
            }
        }

        public TwitchAPI Api
        {
            get
            {
                return api;
            }
        }

        private Timer addPointsTimer;
        private Timer addLurkerPointsTimer;
        private Timer messageRepeater;

        private readonly ILoggerManager _loggerManager;
        private static IDataAccess _dataAccess;
        private static IDataProcessor _dataProcessor;

        public TwitchChatBot(ILoggerManager loggerManager, IDataAccess dataAccess)
        {
            _loggerManager = loggerManager;
            _dataAccess = dataAccess;
        }

        public void Connect()
        {
            Console.WriteLine("Connecting");
            var container = ContainerConfig.Config();
            using (var scope = container.BeginLifetimeScope())
            {
                _dataProcessor = scope.Resolve<IDataProcessor>();
            }
            client = new TwitchClient(logger: _loggerManager.ClientLogger);
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
            addPointsTimer.Elapsed += _dataProcessor.AddPointsTimer_ElapsedAsync;
            addPointsTimer.Start();

            addLurkerPointsTimer = new Timer(1000 * 60 * 10);
            addLurkerPointsTimer.Elapsed += _dataProcessor.AddLurkerPointsTimer_ElapsedAsync;
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
        }

        private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, "/me Incearca !points si vezi cat de activ ai fost");
        }

        private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
        {
            Console.WriteLine($" - - - sent channel: {e.SentMessage.Channel}");
        }

        private void ApiInitialize()
        {
            api = new TwitchAPI(loggerFactory: _loggerManager.APILoggerFactory);
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

        public void Disconnect()
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
                        string[] results = await _dataProcessor.GetPointsMinutesAsync(e.Command.ChatMessage.UserId).ConfigureAwait(false);
                        if (results != null)
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ChatMessage.DisplayName} You have: {results[0]} points and { Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 2)} hours! Be active to gain more!\n\r");
                        }
                        else
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ChatMessage.DisplayName} You aren't yet in the database, hang on a little bit more and you'll be added at the next check!\n\r");
                        }
                    }
                    else
                    {
                        string[] results = await _dataProcessor.GetPointsMinutesAsync(await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false)).ConfigureAwait(false);
                        if (results != null)
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ArgumentsAsString.TrimStart('@')} has: {results[0]} points and {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 2)} hours!");
                        }
                        else
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"{e.Command.ArgumentsAsString.TrimStart('@')} isn't yet in the database!");
                        }
                    }
                    Log.Debug("{DisplayName} asked for points!", e.Command.ChatMessage.DisplayName);
                    break;

                case "pointmanage":
                    int pointNumber;
                    string userid;
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                        {
                            if (!(e.Command.ArgumentsAsList.Count < 2) && int.TryParse(e.Command.ArgumentsAsList[1], out pointNumber) && (userid = await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@')).ConfigureAwait(false)) != null)
                            {
                                await _dataAccess.AddPointToUserID(userid, pointNumber).ConfigureAwait(false);
                                Client.SendMessage(e.Command.ChatMessage.Channel, $"Modified points of {e.Command.ArgumentsAsList[0]} with {e.Command.ArgumentsAsList[1]}");
                            }
                            else
                            {
                                Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PointManageText);
                            }
                        }
                        else
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PointManageText);
                        }
                    }
                    break;

                default:
                    Console.WriteLine($" - - {e.Command.ChatMessage.DisplayName} used an unknow command!(!{e.Command.CommandText})");
                    break;
            }
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine($"Error!!! {e.Error}");
            Log.Error("Error!!! {ErrorMessage}  {ErrorExcenption}", e.Error.Message, e.Error.Exception.Message);
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
                    client.SendMessage(e.ChatMessage.Channel, (await _dataProcessor.GetUptimeAsync().ConfigureAwait(false))?.ToString() ?? "Offline");
                }
            }
        }
    }
}