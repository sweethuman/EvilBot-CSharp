using Autofac;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
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
            switch (e.Command.CommandText.ToLower())
            {
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
                            client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ChatMessage.DisplayName} You aren't yet in the database. You'll be added at the next minute check!");
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
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    break;

                case "manage":
                    string userid;
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                        {
                            if (!(e.Command.ArgumentsAsList.Count < 2) && (userid = await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@')).ConfigureAwait(false)) != null)
                            {
                                int pointModifier = 0;
                                int minuteModifier = 0;
                                bool twoParams = false;
                                bool error = false;
                                List<string> parameters = new List<string>() { e.Command.ArgumentsAsList[1] };
                                if (e.Command.ArgumentsAsList.Count == 3)
                                {
                                    twoParams = true;
                                    parameters = CommandHelpers.ManageCommandSorter(e.Command.ArgumentsAsList[1], e.Command.ArgumentsAsList[2]);
                                }
                                if (parameters[0].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    parameters[0] = parameters[0].TrimEnd(new char[] { 'm', 'M' });

                                    if (!int.TryParse(parameters[0], out minuteModifier))
                                    {
                                        error = true;
                                        Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                    }
                                }
                                else
                                {
                                    if (!int.TryParse(parameters[0], out pointModifier))
                                    {
                                        error = true;
                                        Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                    }
                                }

                                if (twoParams && !error)
                                {
                                    if (parameters[1].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
                                    {
                                        parameters[1] = parameters[1].TrimEnd(new char[] { 'm', 'M' });

                                        if (!int.TryParse(parameters[1], out minuteModifier))
                                        {
                                            error = true;
                                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                        }
                                    }
                                    else
                                    {
                                        error = true;
                                        Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                    }
                                }
                                if (!error)
                                {
                                    await _dataAccess.ModifierUserIDAsync(userid, pointModifier, minuteModifier).ConfigureAwait(false);
                                    Client.SendMessage(e.Command.ChatMessage.Channel, $"/me Modified {e.Command.ArgumentsAsList[0]} with {pointModifier} points and {minuteModifier} minutes");
                                }
                            }
                            else
                            {
                                Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                            }
                        }
                        else
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                        }
                    }
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    break;

                case "pollcreate":
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString) && !e.Command.ArgumentsAsString.Contains("||"))
                        {
                            string arguments = e.Command.ArgumentsAsString.Trim();
                            arguments = arguments.Trim('|');
                            List<string> options = arguments.Split('|').ToList();
                            for (int i = 0; i < options.Count; i++)
                            {
                                options[i] = options[i].Trim();
                            }
                            if (!(options.Count < 2))
                            {
                                Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollCreate(options)}");
                            }
                            else
                            {
                                Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollCreateText);
                            }
                        }
                        else
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollCreateText);
                        }
                    }
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
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
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
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
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    break;

                case "pollend":
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (_pollManager.PollActive)
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollEnd()}");
                        }
                        else
                        {
                            Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
                        }
                    }
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    break;

                case "comenzi":
                    Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ComenziText);
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
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

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (!e.ChatMessage.Message.StartsWith("!"))
            {
                PointCounter.AddMessagePoint(e.ChatMessage.UserId);
            }
        }
    }
}