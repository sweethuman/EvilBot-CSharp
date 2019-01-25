using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.Trackers.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities;
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
		private readonly ITwitchConnections _twitchConnection;
		private readonly IRankManager _rankManager;

		private readonly List<string> _timedMessages = new List<string>();
		private Timer _addLurkerPointsTimer;
		private Timer _addPointsTimer;

		private Timer _messageRepeater;

		private readonly Dictionary<string, (Func<OnChatCommandReceivedArgs, Task<string>> runner, bool needMod)> _commands =
			new Dictionary<string, (Func<OnChatCommandReceivedArgs, Task<string>> runner, bool needMod)>();

		private string PointRateString { get; }
		private string CommandsString { get; set; }

		private string CommandsModString { get; set; }

		public TwitchChatBot(ITwitchConnections twitchConnection, IDataAccess dataAccess, IDataProcessor dataProcessor,
			ICommandProcessor commandProcessor, IFilterManager filterManager, IConfiguration configuration,
			IApiRetriever apiRetriever, IPresenceCounter presenceCounter, ITalkerCounter talkerCounter,
			IRankManager rankManager)
		{
			_twitchConnection = twitchConnection;
			_dataProcessor = dataProcessor;
			_commandProcessor = commandProcessor;
			_filterManager = filterManager;
			_dataAccess = dataAccess;
			_configuration = configuration;
			_presenceCounter = presenceCounter;
			_talkerCounter = talkerCounter;
			_rankManager = rankManager;
			presenceCounter.MakePresent(apiRetriever.TwitchChannelId);
			PointRateString = string.Format(StandardMessages.PointRateString, configuration.LurkerPoints,
				configuration.LurkerMinutes, configuration.TalkerPoints, configuration.TalkerMinutes);
			Connect();
			CommandsInitializer();
		}

		~TwitchChatBot()
		{
			Log.Debug("Disposing of TwitchChatBot");
			_addLurkerPointsTimer.Dispose();
			_addPointsTimer.Dispose();
			_messageRepeater.Dispose();
		}

		#region Commands Logic
		private async void Client_OnChatCommandReceivedAsync(object sender, OnChatCommandReceivedArgs e)
		{
			var success = _commands.TryGetValue(e.Command.CommandText.ToLower(), out var holder);
			if(success == false) return;
			if ((!holder.needMod || e.Command.ChatMessage.UserType < UserType.Moderator) && holder.needMod) return;
			Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
				e.Command.ChatMessage.Message);
			_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
				await holder.runner(e).ConfigureAwait(false));
		}

		private Task<string> AboutCommandAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult($"/me {StandardMessages.BotInformation.AboutBot}");

		private Task<string> ChangelogCommandAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult($"/me {StandardMessages.BotInformation.ChangelogBot}");

		private Task<string> CommandsCommandAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult(CommandHelpers.ChangeOutputIfMod(e.Command.ChatMessage.UserType, CommandsString, CommandsModString));

		private Task<string> PointRateCommandAsync(OnChatCommandReceivedArgs e) => Task.FromResult(PointRateString);

		private Task<string> RankCommandAsync(OnChatCommandReceivedArgs e) => _commandProcessor.RankCommandAsync(e);

		private Task<string> RankListCommandAsync(OnChatCommandReceivedArgs e) =>
			Task.FromResult($"/me {_commandProcessor.RankListString}");

		private Task<string> ManageCommandAsync(OnChatCommandReceivedArgs e) => _commandProcessor.ManageCommandAsync(e);

		private Task<string> FilterCommandAsync(OnChatCommandReceivedArgs e) => _commandProcessor.FilterCommandAsync(e);

		private Task<string> TopCommandAsync(OnChatCommandReceivedArgs e) => _commandProcessor.TopCommandAsync(e);

		private async Task<string> GiveawayCommandAsync(OnChatCommandReceivedArgs e)
		{
			var (usersAnnouncement, winnerAnnouncement) = await _commandProcessor.GiveawayCommandAsync(e).ConfigureAwait(false);
			Log.Debug(usersAnnouncement);
			Log.Debug(winnerAnnouncement);
			_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, usersAnnouncement);
			return winnerAnnouncement;
		}


		#endregion Commands Logic

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
			_timedMessages.Add("Incearca !rank si vezi cat de activ ai fost");
			_timedMessages.Add("Fii activ ca sa castigi XP");
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
			_timedMessages.Add("Subscriberii primesc x2 puncte!");
			_timedMessages.Add("Daca iti place, apasa butonul de FOLLOW! Multumesc pentru sustinere!");
			//_timedMessages.Add("Pentru a migra punctele te rog da !myrank si tag unui moderator");
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
			_twitchConnection.Client.OnChatCommandReceived += Client_OnChatCommandReceivedAsync;
			_twitchConnection.Client.OnMessageReceived += Client_OnMessageReceived;
			_rankManager.RankUpdated += _dataProcessor_RankUpdated;
			_twitchConnection.Client.OnMessageSent += Client_OnMessageSent;
		}

		private void CommandsInitializer()
		{
			_commands.Add("rank",		(RankCommandAsync, false));
			_commands.Add("ranklist",	(RankListCommandAsync, false));
			_commands.Add("ranks",		(RankListCommandAsync, false));
			_commands.Add("top",		(TopCommandAsync, false));
			_commands.Add("pointrate",	(PointRateCommandAsync, false));
			_commands.Add("poll", 		(_commandProcessor.PollCommandAsync, false));
			_commands.Add("giveaway",	(GiveawayCommandAsync, true));
			_commands.Add("manage",		(ManageCommandAsync, true));
			_commands.Add("filter",		(FilterCommandAsync, true));
			_commands.Add("help",	(CommandsCommandAsync, false));
			_commands.Add("about",		(AboutCommandAsync, false));
			_commands.Add("changelog",	(ChangelogCommandAsync, false));

			var commandsBuilder = new StringBuilder();
			var commandsModBuilder = new StringBuilder();
			commandsBuilder.Append("/me Comenzi:");
			commandsModBuilder.Append("/me Comenzi mod:");
			foreach (var command in _commands)
			{
				commandsModBuilder.AppendFormat(" !{0}", command.Key);
				if (command.Value.needMod) commandsModBuilder.Append("(mod)");
				if (!command.Value.needMod) commandsBuilder.AppendFormat(" !{0}", command.Key);
			}
			Log.Debug("CommandsString generated: {0}", commandsBuilder.ToString());
			Log.Debug("CommandsModString generated: {0}", commandsModBuilder.ToString());
			CommandsString = commandsBuilder.ToString();
			CommandsModString = commandsModBuilder.ToString();
		}

		#endregion TwitchChatBot Initializers

		#region TwitchChatBot EventTriggers

		private void _dataProcessor_RankUpdated(object sender, RankUpdateEventArgs e)
		{
			_twitchConnection.Client.SendMessage(_configuration.ChannelName, $"/me {e.Name} ai avansat la {e.Rank}");
		}

		private void MessageRepeater_Elapsed(object sender, ElapsedEventArgs e)
		{
			var rnd = new Random();
			_twitchConnection.Client.SendMessage(_configuration.ChannelName,
				$"/me {_timedMessages[rnd.Next(0, _timedMessages.Count)]}");
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
					$"/me {e.ChatMessage.DisplayName} a fost recompensat {e.ChatMessage.Bits * _configuration.BitsPointsMultiplier + 11} puncte! Bravo!";
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
