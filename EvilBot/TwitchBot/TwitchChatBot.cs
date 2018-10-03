using EvilBot.DataStructures;
using EvilBot.Processors;
using Serilog;
using System;
using System.Collections.Generic;
using System.Configuration;
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

        private static IDataProcessor _dataProcessor;
        private static ITwitchConnections _twitchConnection;
        private static ICommandProcessor _commandProcessor;

        public List<string> timedMessages = new List<string>();

        public TwitchChatBot(ITwitchConnections twitchConnection, IDataProcessor dataProcessor, ICommandProcessor commandProcessor)
        {
            _twitchConnection = twitchConnection;
            _dataProcessor = dataProcessor;
            _commandProcessor = commandProcessor;
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
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, await _commandProcessor.RankCommandAsync(e).ConfigureAwait(false));
                    break;

                case "manage":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, await _commandProcessor.ManageCommandAsync(e).ConfigureAwait(false));
                    }
                    break;

                case "pollcreate":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, _commandProcessor.PollCreateCommand(e));
                    }
                    break;

                case "pollvote":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, await _commandProcessor.PollVoteCommandAsync(e).ConfigureAwait(false));
                    break;

                case "pollstats":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, _commandProcessor.PollStatsCommand(e));
                    break;

                case "pollend":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= TwitchLib.Client.Enums.UserType.Moderator)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, _commandProcessor.PollEndCommand(e));
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