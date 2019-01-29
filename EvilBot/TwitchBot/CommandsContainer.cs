using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities;
using Serilog;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
	public class CommandsContainer
	{
		private readonly Dictionary<string, (Func<OnChatCommandReceivedArgs, Task<string>> runner, bool needMod)> _commands =
			new Dictionary<string, (Func<OnChatCommandReceivedArgs, Task<string>> runner, bool needMod)>();

		private readonly ICommandProcessor _commandProcessor;
		private readonly ITwitchConnections _twitchConnection;

		private string PointRateString { get; }
		private string CommandsString { get; set; }
		private string CommandsModString { get; set; }


		public CommandsContainer(ICommandProcessor commandProcessor, IConfiguration configuration, ITwitchConnections twitchConnection)
		{
			_commandProcessor = commandProcessor;
			_twitchConnection = twitchConnection;
			_twitchConnection.Client.OnChatCommandReceived += Client_OnChatCommandReceivedAsync;
			PointRateString = string.Format(StandardMessages.PointRateString, configuration.LurkerPoints,
				configuration.LurkerMinutes, configuration.TalkerPoints, configuration.TalkerMinutes);
			CommandsInitializer();
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
	}
}
