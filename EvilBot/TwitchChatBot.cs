using Autofac;
using Serilog;
using System;
using System.Collections.Generic;
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
        private static IPollManager _pollManager;

        public List<string> timedMessages = new List<string>();

        public TwitchChatBot(ILoggerManager loggerManager, IDataAccess dataAccess, IPollManager pollManager)
        {
            _loggerManager = loggerManager;
            _dataAccess = dataAccess;
            _pollManager = pollManager;
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

            TimedMessageInitializer();
            TimerInitializer();
        }

        private void TimedMessageInitializer()
        {
            timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
            timedMessages.Add("Fii activ ca sa castigi XP");
            timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
            timedMessages.Add("Subcriberii castiga triplu de puncte!");
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
            _dataProcessor.RankUpdated += _dataProcessor_RankUpdated;
        }

        private void _dataProcessor_RankUpdated(object sender, RankUpdateEventArgs e)
        {
            client.SendMessage(TwitchInfo.ChannelName, $"/me {e.Name} ai avansat la {e.Rank}");
        }

        private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
        {
            Random rnd = new Random();
            client.SendMessage(TwitchInfo.ChannelName, $"/me {timedMessages[rnd.Next(0, timedMessages.Count)]}");
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

                case "rank":
                    if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                    {
                        List<string> results = await _dataProcessor.GetUserAttributesAsync(e.Command.ChatMessage.UserId).ConfigureAwait(false);
                        if (results != null)
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ChatMessage.DisplayName} esti {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!\n\r");
                        }
                        else
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ChatMessage.DisplayName} You aren't yet in the database, hang on a little bit more and you'll be added at the next check!\n\r");
                        }
                    }
                    else
                    {
                        List<string> results = await _dataProcessor.GetUserAttributesAsync(await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false)).ConfigureAwait(false);
                        if (results != null)
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ArgumentsAsString.TrimStart('@')} este {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!");
                        }
                        else
                        {
                            client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ArgumentsAsString.TrimStart('@')} isn't yet in the database!");
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
                                await _dataAccess.ModifierUserIDAsync(userid, pointNumber).ConfigureAwait(false);
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

                case "pollcreate":
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString) && !(e.Command.ArgumentsAsList.Count < 2))
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollCreate(e.Command.ArgumentsAsList)}");
                        }
                        else
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollCreateText);
                        }
                    }
                    break;

                case "pollvote":
                    if (_pollManager.PollActive)
                    {
                        if (int.TryParse(e.Command.ArgumentsAsString, out int votedNumber))
                        {
                            await _pollManager.PollAddVote(e.Command.ChatMessage.UserId, votedNumber);
                        }
                        else
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollVoteText);
                        }
                    }
                    else
                    {
                        Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
                    }
                    break;

                case "pollstats":
                    if (_pollManager.PollActive)
                    {
                        Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollStats()}");
                    }
                    else
                    {
                        Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
                    }
                    break;

                case "pollend":
                    if (_pollManager.PollActive)
                    {
                        Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollEnd()}");
                    }
                    else
                    {
                        Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
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