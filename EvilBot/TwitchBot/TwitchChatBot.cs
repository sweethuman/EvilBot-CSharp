using EvilBot.DataStructures;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Timers;
using TwitchLib.Client.Events;

namespace EvilBot
{
    internal class TwitchChatBot : ITwitchChatBot
    {
        private Timer addPointsTimer;
        private Timer addLurkerPointsTimer;
        private Timer messageRepeater;
        private float messageRepeaterMinutes;

        private static IDataAccess _dataAccess;
        private static IDataProcessor _dataProcessor;
        private static IPollManager _pollManager;
        private static ITwitchConnections _twitchConnection;

        public List<string> timedMessages = new List<string>();

        public TwitchChatBot(IDataAccess dataAccess, IPollManager pollManager, ITwitchConnections twitchConnection, IDataProcessor dataProcessor)
        {
            _dataAccess = dataAccess;
            _pollManager = pollManager;
            _twitchConnection = twitchConnection;
            _dataProcessor = dataProcessor;
            messageRepeaterMinutes = float.Parse(ConfigurationManager.AppSettings.Get("messageRepeaterMinutes"));
        }

        #region TwitchChatBot Initializers

        public void Connect()
        {
            Log.Debug("Starting EvilBot");

            EventIntializer();

            TimedMessageInitializer();
            TimerInitializer();
        }

        public void Disconnect()
        {
            Log.Debug("Disconnecting");
        }

        private void TimedMessageInitializer()
        {
            timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
            timedMessages.Add("Fii activ ca sa castigi XP");
            timedMessages.Add("Subscriberii primesc x2 puncte!");
            timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
        }

        private void TimerInitializer()
        {
            addPointsTimer = new Timer(1000 * 60 * 1);
            addPointsTimer.Elapsed += _dataProcessor.AddPointsTimer_ElapsedAsync;
            addPointsTimer.Start();

            addLurkerPointsTimer = new Timer(1000 * 60 * 10);
            addLurkerPointsTimer.Elapsed += _dataProcessor.AddLurkerPointsTimer_ElapsedAsync;
            addLurkerPointsTimer.Start();

            messageRepeater = new Timer(1000 * 60 * messageRepeaterMinutes);
            messageRepeater.Elapsed += MessageRepeater_Elapsed;
            messageRepeater.Start();
        }

        private void EventIntializer()
        {
            _twitchConnection.Client.OnConnectionError += Client_OnConnectionError;
            _twitchConnection.Client.OnChatCommandReceived += Client_OnChatCommandReceived;
            _twitchConnection.Client.OnMessageReceived += Client_OnMessageReceived;
            _dataProcessor.RankUpdated += _dataProcessor_RankUpdated;
        }

        #endregion TwitchChatBot Initializers

        #region TwitchChatBot EventTriggers

        private void _dataProcessor_RankUpdated(object sender, RankUpdateEventArgs e)
        {
            _twitchConnection.Client.SendMessage(TwitchInfo.ChannelName, $"/me {e.Name} ai avansat la {e.Rank}");
        }

        private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
        {
            Random rnd = new Random();
            _twitchConnection.Client.SendMessage(TwitchInfo.ChannelName, $"/me {timedMessages[rnd.Next(0, timedMessages.Count)]}");
        }

        private void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Log.Error("Error!!! {ErrorMessage}", e.Error.Message);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (!e.ChatMessage.Message.StartsWith("!"))
            {
                PointCounter.AddMessagePoint(new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId));
            }
        }

        #endregion TwitchChatBot EventTriggers

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            switch (e.Command.CommandText.ToLower())
            {
                case "colorme":
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, "/color Red");
                    break;

                case "rank":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
                    {
                        List<string> results = await _dataProcessor.GetUserAttributesAsync(e.Command.ChatMessage.UserId).ConfigureAwait(false);
                        if (results != null)
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ChatMessage.DisplayName} esti {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!\n\r");
                        }
                        else
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ChatMessage.DisplayName} You aren't yet in the database. You'll be added at the next minute check!");
                        }
                    }
                    else
                    {
                        List<string> results = await _dataProcessor.GetUserAttributesAsync(await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false)).ConfigureAwait(false);
                        if (results != null)
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ArgumentsAsString.TrimStart('@')} este {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!");
                        }
                        else
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {e.Command.ArgumentsAsString.TrimStart('@')} isn't yet in the database!");
                        }
                    }
                    break;

                case "manage":
                    string userid;
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
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
                                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                    }
                                }
                                else
                                {
                                    if (!int.TryParse(parameters[0], out pointModifier))
                                    {
                                        error = true;
                                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
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
                                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                        }
                                    }
                                    else
                                    {
                                        error = true;
                                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                                    }
                                }
                                if (!error)
                                {
                                    await _dataAccess.ModifierUserIDAsync(userid, pointModifier, minuteModifier).ConfigureAwait(false);
                                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me Modified {e.Command.ArgumentsAsList[0]} with {pointModifier} points and {minuteModifier} minutes");
                                }
                            }
                            else
                            {
                                _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                            }
                        }
                        else
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ManageCommandText);
                        }
                    }
                    break;

                case "pollcreate":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString) && !e.Command.ArgumentsAsString.Contains("||"))
                        {
                            string arguments = e.Command.ArgumentsAsString.Trim();
                            arguments = arguments.Trim('|');
                            arguments = arguments.Trim();
                            List<string> options = arguments.Split('|').ToList();
                            for (int i = 0; i < options.Count; i++)
                            {
                                options[i] = options[i].Trim();
                            }
                            if (!(options.Count < 2))
                            {
                                _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollCreate(options)}");
                            }
                            else
                            {
                                _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollCreateText);
                            }
                        }
                        else
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollCreateText);
                        }
                    }
                    break;

                case "pollvote":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (_pollManager.PollActive)
                    {
                        if (int.TryParse(e.Command.ArgumentsAsString, out int votedNumber))
                        {
                            await _pollManager.PollAddVote(e.Command.ChatMessage.UserId, votedNumber);
                        }
                        else
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollVoteText);
                        }
                    }
                    else
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
                    }
                    break;

                case "pollstats":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (_pollManager.PollActive)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollStats()}");
                    }
                    else
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
                    }
                    break;

                case "pollend":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        if (_pollManager.PollActive)
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, $"/me {_pollManager.PollEnd()}");
                        }
                        else
                        {
                            _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.PollNotActiveText);
                        }
                    }
                    break;

                case "comenzi":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ComenziText);
                    break;

                default:
                    break;
            }
        }
    }
}