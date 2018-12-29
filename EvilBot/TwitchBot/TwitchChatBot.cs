using System;
using System.Collections.Generic;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities.Interfaces;
using Serilog;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
	internal class TwitchChatBot : ITwitchChatBot
	{
		private readonly ICommandProcessor _commandProcessor;
		private readonly IConfiguration _configuration;
		private readonly IDataAccess _dataAccess;
		private readonly IDataProcessor _dataProcessor;
		private readonly IFilterManager _filterManager;
		private readonly IPresenceCounter _presenceCounter;
		private readonly ITalkerCounter _talkerCounter;

		private readonly List<string> _timedMessages = new List<string>();
		private readonly ITwitchConnections _twitchConnection;
		private Timer _addLurkerPointsTimer;
		private Timer _addPointsTimer;

		private int _bitsToPointsMultiplier;
		private Timer _messageRepeater;
		private float _messageRepeaterMinutes;

		//TODO decrease the number of dependencies
		public TwitchChatBot(ITwitchConnections twitchConnection, IDataAccess dataAccess, IDataProcessor dataProcessor,
			ICommandProcessor commandProcessor, IFilterManager filterManager, IConfiguration configuration,
			IApiRetriever apiRetriever, IPresenceCounter presenceCounter, ITalkerCounter talkerCounter)
		{
			_twitchConnection = twitchConnection;
			_dataProcessor = dataProcessor;
			_commandProcessor = commandProcessor;
			_filterManager = filterManager;
			_dataAccess = dataAccess;
			_configuration = configuration;
			_presenceCounter = presenceCounter;
			_talkerCounter = talkerCounter;
			presenceCounter.MakePresent(apiRetriever.TwitchChannelId);
			Connect();
		}

		~TwitchChatBot()
		{
			Log.Debug("Disposing of TwitchChatBot");
			_addLurkerPointsTimer.Dispose();
			_addPointsTimer.Dispose();
			_messageRepeater.Dispose();
		}

		private async void Client_OnChatCommandReceivedAsync(object sender, OnChatCommandReceivedArgs e)
		{
			switch (e.Command.CommandText.ToLower())
			{
				case "colorme":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
						$"/color {e.Command.ArgumentsAsString}");
					break;

				case "rank":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
						await _commandProcessor.RankCommandAsync(e).ConfigureAwait(false));
					break;
				case "ranks":
				case "ranklist":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
						_commandProcessor.RanksListCommand(e));
					break;
				case "manage":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					if (e.Command.ChatMessage.UserType >= UserType.Moderator)
						_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
							await _commandProcessor.ManageCommandAsync(e).ConfigureAwait(false));
					break;

				case "pollcreate":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					if (e.Command.ChatMessage.UserType >= UserType.Moderator)
						_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
							_commandProcessor.PollCreateCommand(e));
					break;

				case "pollvote":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
						await _commandProcessor.PollVoteCommandAsync(e).ConfigureAwait(false));
					break;

				case "pollstats":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
						_commandProcessor.PollStatsCommand(e));
					break;

				case "pollend":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					if (e.Command.ChatMessage.UserType >= UserType.Moderator)
						_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
							_commandProcessor.PollEndCommand(e));
					break;

				case "comenzi":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, StandardMessages.ComenziText);
					break;

				case "filter":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					if (e.Command.ChatMessage.UserType >= UserType.Moderator)
						_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
							await _commandProcessor.FilterCommandAsync(e).ConfigureAwait(false));
					break;
				case "top":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
						await _commandProcessor.TopCommandAsync(e).ConfigureAwait(false));
					break;
				case "giveaway":
					Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
						e.Command.ChatMessage.Message);
					if (e.Command.ChatMessage.UserType >= UserType.Moderator)
					{
						var giveaway = await _commandProcessor.GiveawayCommandAsync(e).ConfigureAwait(false);
						Log.Debug(giveaway.usersAnnouncement);
						Log.Debug(giveaway.winnerAnnouncement);
						_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, giveaway.usersAnnouncement);
						_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
							giveaway.winnerAnnouncement);
					}

					break;
			}
		}

		#region TwitchChatBot Initializers

		public void Connect()
		{
			Log.Debug("Starting EvilBot");
			_messageRepeaterMinutes = _configuration.MessageRepeaterMinutes;
			_bitsToPointsMultiplier = _configuration.BitsPointsMultiplier;

			EventInitializer();
			TimedMessageInitializer();
			TimerInitializer();
			_filterManager.InitializeFilterAsync().Wait();
		}

		public void Disconnect()
		{
			_dataAccess.Close();
			Log.Debug("Disconnecting");
		}

		private void TimedMessageInitializer()
		{
			_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			_timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
			_timedMessages.Add("Fii activ ca sa castigi XP");
			_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			_timedMessages.Add("Subscriberii primesc x2 puncte!");
			_timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
			_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
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

		private void EventInitializer()
		{
			_twitchConnection.Client.OnConnectionError += Client_OnConnectionError;
			_twitchConnection.Client.OnChatCommandReceived += Client_OnChatCommandReceivedAsync;
			_twitchConnection.Client.OnMessageReceived += Client_OnMessageReceived;
			_dataProcessor.RankUpdated += _dataProcessor_RankUpdated;
			_twitchConnection.Client.OnMessageSent += Client_OnMessageSent;
		}

		private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
		{
			Log.Information("Message sent: {message}", e.SentMessage.Message);
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
			_twitchConnection.Client.SendMessage(TwitchInfo.ChannelName,
				$"/me {_timedMessages[rnd.Next(0, _timedMessages.Count)]}");
		}

		private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
		{
			Log.Error("Error!!! {ErrorMessage}", e.Error.Message);
		}

		private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
		{
			if (!e.ChatMessage.Message.StartsWith("!"))
				if (!_filterManager.CheckIfUserIdFiltered(e.ChatMessage.UserId))
				{
					_talkerCounter.AddTalker(new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId));
					if (!_presenceCounter.CheckIfPresent(e.ChatMessage.UserId))
					{
						_presenceCounter.MakePresent(e.ChatMessage.UserId);
						_twitchConnection.Client.SendMessage(e.ChatMessage.Channel,
							$"/me Bine ai venit {e.ChatMessage.DisplayName}!");
					}
				}

			if (e.ChatMessage.Bits == 0) return;
			string message;
			try
			{
				_dataProcessor.AddToUserAsync(
					new List<IUserBase> {new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId)},
					e.ChatMessage.Bits * _bitsToPointsMultiplier + 11, subCheck: false);
				message =
					$"/me {e.ChatMessage.DisplayName} a fost recompensat {e.ChatMessage.Bits * _bitsToPointsMultiplier + 11} puncte! Bravo!";
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Rewarding user for bits FAILED!");
				message = "/me A ESUAT SA RECOMPENSEZE USERul. SEND LOGS.";
			}

			_twitchConnection.Client.SendMessage(e.ChatMessage.Channel, message);
		}

		#endregion TwitchChatBot EventTriggers
	}
}
