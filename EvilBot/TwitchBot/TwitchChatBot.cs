using System;
using System.Collections.Generic;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources.Interfaces;
using EvilBot.Trackers.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
	internal class TwitchChatBot : ITwitchChatBot
	{
		private readonly IConfiguration _configuration;
		private readonly IDataAccess _dataAccess;
		private readonly IDataProcessor _dataProcessor;
		private readonly IFilterManager _filterManager;
		private readonly IPresenceCounter _presenceCounter;
		private readonly ITalkerCounter _talkerCounter;
		private readonly ITwitchConnections _twitchConnection;
		private readonly IRankManager _rankManager;
		private readonly Random _random = new Random();

		private readonly List<string> _timedMessages = new List<string>();
		private Timer _addLurkerPointsTimer;
		private Timer _addPointsTimer;

		private Timer _messageRepeater;

		public TwitchChatBot(ITwitchConnections twitchConnection, IDataAccess dataAccess, IDataProcessor dataProcessor,
			IFilterManager filterManager, IConfiguration configuration, IApiRetriever apiRetriever,
			IPresenceCounter presenceCounter, ITalkerCounter talkerCounter, IRankManager rankManager)
		{
			_twitchConnection = twitchConnection;
			_dataProcessor = dataProcessor;
			_filterManager = filterManager;
			_dataAccess = dataAccess;
			_configuration = configuration;
			_presenceCounter = presenceCounter;
			_talkerCounter = talkerCounter;
			_rankManager = rankManager;
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

		#region TwitchChatBot Initializers

		public void Connect()
		{
			Log.Debug("Starting EvilBot");

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
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			_timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
			_timedMessages.Add("Fii activ ca sa castigi XP");
			_timedMessages.Add("Subscriberii primesc x2 XP!");
			_timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
			_timedMessages.Add("Joaca la !gamble ca sa iti dublezi XP-ul!");
			_timedMessages.Add("Joaca la !gamble ca sa iti dublezi XP-ul!");
		}

		private void TimerInitializer()
		{
			_addPointsTimer = new Timer(1000 * 60 * _configuration.TalkerMinutes);
			_addPointsTimer.Elapsed += _dataProcessor.AddPointsTimer_ElapsedAsync;
			_addPointsTimer.Start();

			_addLurkerPointsTimer = new Timer(1000 * 60 * _configuration.LurkerMinutes);
			_addLurkerPointsTimer.Elapsed += _dataProcessor.AddLurkerPointsTimer_ElapsedAsync;
			_addLurkerPointsTimer.Start();

			_messageRepeater = new Timer(1000 * 60 * _configuration.MessageRepeaterMinutes);
			_messageRepeater.Elapsed += MessageRepeater_Elapsed;
			_messageRepeater.Start();
		}

		private void EventInitializer()
		{
			_twitchConnection.Client.OnConnectionError += Client_OnConnectionError;
			_twitchConnection.Client.OnMessageReceived += Client_OnMessageReceived;
			_rankManager.RankUpdated += _dataProcessor_RankUpdated;
			_twitchConnection.Client.OnMessageSent += Client_OnMessageSent;
		}

		#endregion TwitchChatBot Initializers

		#region TwitchChatBot EventTriggers

		private void _dataProcessor_RankUpdated(object sender, RankUpdateEventArgs e)
		{
			_twitchConnection.Client.SendMessage(_configuration.ChannelName, $"/me {e.Name} ai avansat la {e.Rank}");
		}

		private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
		{
			_twitchConnection.Client.SendMessage(_configuration.ChannelName,
				$"/me {_timedMessages[_random.Next(0, _timedMessages.Count)]}");
		}

		private static void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
		{
			Log.Error("Error!!! {ErrorMessage}", e.Error.Message);
		}

		private void Client_OnMessageSent(object sender, OnMessageSentArgs e)
		{
			Log.Information("Message sent: {message}", e.SentMessage.Message);
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
					e.ChatMessage.Bits * _configuration.BitsPointsMultiplier + 11, subCheck: false);
				message =
					$"/me {e.ChatMessage.DisplayName} a fost recompensat {e.ChatMessage.Bits * _configuration.BitsPointsMultiplier + 11}XP! Bravo!";
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
