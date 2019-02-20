using System;
using System.Collections.Generic;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.Trackers.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
	public class MessageHandler
	{

		private readonly IRankManager _rankManager;
		private readonly IDataAccess _dataAccess;
		private readonly ITwitchConnections _twitchConnection;
		private readonly IConfiguration _configuration;
		private readonly IDataProcessor _dataProcessor;
		private readonly IFilterManager _filterManager;
		private readonly IPresenceCounter _presenceCounter;
		private readonly ITalkerCounter _talkerCounter;

		public MessageHandler(IRankManager rankManager, IDataAccess dataAccess, ITwitchConnections twitchConnections,
			IConfiguration configuration, IDataProcessor dataProcessor, IFilterManager filterManager,
			IPresenceCounter presenceCounter, ITalkerCounter talkerCounter)
		{
			_rankManager = rankManager;
			_dataAccess = dataAccess;
			_twitchConnection = twitchConnections;
			_configuration = configuration;
			_dataProcessor = dataProcessor;
			_filterManager = filterManager;
			_presenceCounter = presenceCounter;
			_talkerCounter = talkerCounter;
			EventInitializer();
			TimedMessageInitializer();
		}

		private readonly List<string> _timedMessages = new List<string>();
		private Timer _messageRepeater;
		private readonly Random _random = new Random();

		private void EventInitializer()
		{
			_twitchConnection.Client.OnMessageReceived += WelcomeMessageCheck;
			_twitchConnection.Client.OnMessageReceived += BitsCheck;
			_rankManager.RankUpdated += RankUpdated;
		}


		private void TimedMessageInitializer()
		{
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			_timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
			_timedMessages.Add("Fii activ ca sa castigi XP");
			_timedMessages.Add("Subscriberii primesc X2 XP!");
			_timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
			_timedMessages.Add("Joaca la !gamble ca sa iti dublezi XP-ul!");
			_timedMessages.Add("Joaca la !gamble ca sa iti dublezi XP-ul!");

			_messageRepeater = new Timer(1000 * 60 * _configuration.MessageRepeaterMinutes);
			_messageRepeater.Elapsed += MessageRepeater_Elapsed;
			_messageRepeater.Start();
		}

		private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
		{
			_twitchConnection.Client.SendMessage(_configuration.ChannelName,
				$"/me {_timedMessages[_random.Next(0, _timedMessages.Count)]}");
		}

		private void RankUpdated(object sender, RankUpdateEventArgs e)
		{
			_twitchConnection.Client.SendMessage(_configuration.ChannelName, $"/me {e.Name} ai avansat la {e.Rank}");
		}

		private async void WelcomeMessageCheck(object sender, OnMessageReceivedArgs e)
		{
			if (e.ChatMessage.Message.StartsWith("!")) return;
			if (_filterManager.CheckIfUserIdFiltered(e.ChatMessage.UserId)) return;
			_talkerCounter.AddTalker(new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId));
			if (_presenceCounter.CheckIfPresent(e.ChatMessage.UserId)) return;
			_presenceCounter.MakePresent(e.ChatMessage.UserId);
			var user = await _dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, e.ChatMessage.UserId).ConfigureAwait(false);
			if(user == null)
			{
				_twitchConnection.Client.SendMessage(e.ChatMessage.Channel,
					$"/me Bine ai venit {e.ChatMessage.DisplayName}!");
				return;
			}

			if (!int.TryParse(user.Rank, out var userRank))
			{
				_twitchConnection.Client.SendMessage(e.ChatMessage.Channel,
					"/me Could not parse rank. Send LOGS!");
				Log.Error("Could Not Parse Rank {rank}, {user}", userRank, user);
				return;
			}
			_twitchConnection.Client.SendMessage(e.ChatMessage.Channel,
				$"/me Bine ai venit {_rankManager.GetRank(userRank).Name} {e.ChatMessage.DisplayName}!");
		}

		private async void BitsCheck(object sender, OnMessageReceivedArgs e)
		{
			if (e.ChatMessage.Bits == 0) return;
			string message;
			try
			{
				await _dataProcessor.AddToUserAsync(
					new List<IUserBase> {new UserBase(e.ChatMessage.DisplayName, e.ChatMessage.UserId)},
					e.ChatMessage.Bits * _configuration.BitsPointsMultiplier + 11, subCheck: false).ConfigureAwait(false);
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
	}
}
