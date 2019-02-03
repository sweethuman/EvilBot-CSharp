using System.Collections.Generic;
using System.Text;
using Autofac;
using EvilBot.TwitchBot.Commands;
using EvilBot.TwitchBot.Commands.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot
{
	public class CommandsContainer
	{
		private readonly Dictionary<string, ITwitchCommand> _commands =
			new Dictionary<string, ITwitchCommand>();

		private readonly ITwitchConnections _twitchConnection;


		public CommandsContainer(ITwitchConnections twitchConnection)
		{
			_twitchConnection = twitchConnection;
			_twitchConnection.Client.OnChatCommandReceived += Client_OnChatCommandReceivedAsync;
			CommandsInitializer();
		}

		private void CommandsInitializer()
		{
			using (var scope = ContainerConfig.Container.BeginLifetimeScope())
			{
				_commands.Add("rank", scope.Resolve<RankCommand>());
				_commands.Add("ranklist", scope.Resolve<RankListCommand>());
				_commands.Add("ranks", scope.Resolve<RankListCommand>());
				_commands.Add("top", scope.Resolve<TopCommand>());
				_commands.Add("pointrate", scope.Resolve<PointRateCommand>());
				_commands.Add("bet", scope.Resolve<BetCommand>());
				_commands.Add("poll", scope.Resolve<PollCommand>());
				_commands.Add("giveaway", scope.Resolve<GiveawayCommand>());
				_commands.Add("manage", scope.Resolve<ManageCommand>());
				_commands.Add("filter", scope.Resolve<FilterCommand>());
				_commands.Add("about", scope.Resolve<AboutCommand>());
				_commands.Add("changelog", scope.Resolve<ChangelogCommand>());

				var commandsBuilder = new StringBuilder();
				var commandsModBuilder = new StringBuilder();
				commandsBuilder.Append("/me Comenzi:");
				commandsModBuilder.Append("/me Comenzi mod:");
				foreach (var command in _commands)
				{
					commandsModBuilder.AppendFormat(" !{0}", command.Key);
					if (command.Value.NeedMod) commandsModBuilder.Append("(mod)");
					if (!command.Value.NeedMod) commandsBuilder.AppendFormat(" !{0}", command.Key);
				}

				Log.Debug("CommandsString generated: {0}", commandsBuilder.ToString());
				Log.Debug("CommandsModString generated: {0}", commandsModBuilder.ToString());

				_commands.Add("help", scope.Resolve<HelpCommand>(
					new NamedParameter("commandsString", commandsBuilder.ToString()),
					new NamedParameter("commandsModString", commandsModBuilder.ToString())));
			}
		}

		#region Commands Logic

		private async void Client_OnChatCommandReceivedAsync(object sender, OnChatCommandReceivedArgs e)
		{
			var success = _commands.TryGetValue(e.Command.CommandText.ToLower(), out var command);
			if (success == false) return;
			if ((!command.NeedMod || e.Command.ChatMessage.UserType < UserType.Moderator) && command.NeedMod) return;
			Log.Verbose("{username}:{message}", e.Command.ChatMessage.DisplayName,
				e.Command.ChatMessage.Message);
			_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel,
				await command.ProcessorAsync(e).ConfigureAwait(false));
		}

		#endregion Commands Logic
	}
}
