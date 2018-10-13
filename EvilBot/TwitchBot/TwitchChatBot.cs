using System;
using System.Collections.Generic;
using System.Configuration;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;
using Serilog;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
    internal class TwitchChatBot : ITwitchChatBot
    {
        private Timer _addPointsTimer;
        private Timer _addLurkerPointsTimer;
        private Timer _messageRepeater;
        private float _messageRepeaterMinutes;

        private int _bitsToPointsMultiplier;

        private readonly IDataProcessor _dataProcessor;
        private readonly ITwitchConnections _twitchConnection;
        private readonly ICommandProcessor _commandProcessor;
        private readonly IFilterManager _filterManager;

        private readonly List<string> _timedMessages = new List<string>();

        public TwitchChatBot(ITwitchConnections twitchConnection, IDataProcessor dataProcessor, ICommandProcessor commandProcessor, IFilterManager filterManager)
        {
            _twitchConnection = twitchConnection;
            _dataProcessor = dataProcessor;
            _commandProcessor = commandProcessor;
            _filterManager = filterManager;
        }

        ~TwitchChatBot()
        {
            Log.Debug("Disposing of TwitchChatBot");
            _addLurkerPointsTimer.Dispose();
            _addPointsTimer.Dispose();
            _messageRepeater.Dispose();
        }

        #region TwitchChatBot Initializers

        public void Connect()
        {
            Log.Debug("Starting EvilBot");
            _messageRepeaterMinutes = float.Parse(ConfigurationManager.AppSettings.Get("messageRepeaterMinutes"));
            if (!int.TryParse(ConfigurationManager.AppSettings.Get("bitsToPointsMultipliers"), out _bitsToPointsMultiplier))
            {
                Log.Error("UNABLE TO PARSE {number} TO BITSPOINTSMULTIPLIER, NOT INT", ConfigurationManager.AppSettings.Get("bitsToPointsMultipliers"));
            }
            
            EventIntializer();
            TimedMessageInitializer();
            TimerInitializer();
            _filterManager.InitializeFilter();
        }

        public void Disconnect()
        {
            Log.Debug("Disconnecting");
        }

        private void TimedMessageInitializer()
        {
            _timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
            _timedMessages.Add("Fii activ ca sa castigi XP");
            _timedMessages.Add("Subscriberii primesc x2 puncte!");
            _timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
        }

        private void TimerInitializer()
        {
            _addPointsTimer = new Timer(1000 * 60 * 1);
            _addPointsTimer.Elapsed += _dataProcessor.AddPointsTimer_ElapsedAsync;
            _addPointsTimer.Start();

            _addLurkerPointsTimer = new Timer(1000 * 60 * 10);
            _addLurkerPointsTimer.Elapsed += _dataProcessor.AddLurkerPointsTimer_ElapsedAsync;
            _addLurkerPointsTimer.Start();

            _messageRepeater = new Timer(1000 * 60 * _messageRepeaterMinutes);
            _messageRepeater.Elapsed += MessageRepeater_Elapsed;
            _messageRepeater.Start();
        }

        private void EventIntializer()
        {
            _twitchConnection.Client.OnConnectionError += Client_OnConnectionError;
            _twitchConnection.Client.OnChatCommandReceived += Client_OnChatCommandReceivedAsync;
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
            var rnd = new Random();
            _twitchConnection.Client.SendMessage(TwitchInfo.ChannelName, $"/me {_timedMessages[rnd.Next(0, _timedMessages.Count)]}");
        }

        private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Log.Error("Error!!! {ErrorMessage}", e.Error.Message);
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            if (!e.ChatMessage.Message.StartsWith("!"))
            {
                PointCounter.AddMessagePoint(new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId));
            }

            if (e.ChatMessage.Bits != 0)
            {
                _dataProcessor.AddToUserAsync(new List<IUserBase> { new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId) }, (e.ChatMessage.Bits * _bitsToPointsMultiplier) + 11, subCheck: false);
                _twitchConnection.Client.SendMessage(e.ChatMessage.Channel, $"/me {e.ChatMessage.DisplayName} HAS BEEN REWARDED {(e.ChatMessage.Bits * _bitsToPointsMultiplier) + 11} POINTS!");
            }
        }

        #endregion TwitchChatBot EventTriggers

        private async void Client_OnChatCommandReceivedAsync(object sender, OnChatCommandReceivedArgs e)
        {
            switch (e.Command.CommandText.ToLower())
            {
                case "colorme":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, "/color Red");
                    break;

                case "rank":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, await _commandProcessor.RankCommandAsync(e).ConfigureAwait(false));
                    break;

                case "manage":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= UserType.Moderator)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, await _commandProcessor.ManageCommandAsync(e).ConfigureAwait(false));
                    }
                    break;

                case "pollcreate":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= UserType.Moderator)
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
                    if (e.Command.ChatMessage.UserType >= UserType.Moderator)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, _commandProcessor.PollEndCommand(e));
                    }
                    break;

                case "comenzi":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ComenziText);
                    break;
                
                case "filter":
                    Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
                    if (e.Command.ChatMessage.UserType >= UserType.Moderator)
                    {
                        _twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, await _commandProcessor.FilterCommand(e));
                    }
                    break;
            }
        }
    }
}