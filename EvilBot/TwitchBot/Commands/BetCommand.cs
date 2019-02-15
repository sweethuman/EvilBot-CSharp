using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EvilBot.Managers;
using EvilBot.Resources;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using EvilBot.Utilities;
using Serilog;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class BetCommand : ITwitchCommand
	{
		private readonly IBetManager _betManager;
		private readonly IApiRetriever _apiRetriever;

		private string BetFormat { get; set; }
		private string BetModFormat { get; set; }

		private readonly Dictionary<string, (Func<OnChatCommandReceivedArgs, Task<string>> processor, bool NeedMod, bool requireActive)> _commands =
			new Dictionary<string, (Func<OnChatCommandReceivedArgs, Task<string>> processor, bool NeedMod, bool requireActive)>();

		public BetCommand(IBetManager betManager, IApiRetriever apiRetriever)
		{
			_betManager = betManager;
			_apiRetriever = apiRetriever;
			CommandsInitializer();
		}

		private void CommandsInitializer()
		{
			_commands.Add("create", (CreateBetAsync, true, false));
			_commands.Add("cancel", (CancelBetAsync, true, false));
			_commands.Add("end", (EndBetAsync, true, false));
			_commands.Add("vote", (MakeVoteAsync, false, true));
			_commands.Add("votecancel", (UndoBetAsync, false, true));
			_commands.Add("openvoting", (OpenBetAsync, true, true));
			_commands.Add("closevoting", (CloseBetAsync, true, true));
			_commands.Add("stats", (StateOfBetAsync, false, true));
			_commands.Add("check", (CheckUserAsync, false, true));

			var commandsBuilder = new StringBuilder();
			var commandsModBuilder = new StringBuilder();
			commandsBuilder.Append("/me !bet ");
			commandsModBuilder.Append("/me !bet ");
			foreach (var command in _commands)
			{
				commandsModBuilder.AppendFormat("{0}/", command.Key);
				if (!command.Value.NeedMod) commandsBuilder.AppendFormat("{0}/", command.Key);
			}

			commandsBuilder.Remove(commandsBuilder.Length - 1, 1);
			commandsModBuilder.Remove(commandsModBuilder.Length - 1, 1);

			Log.Debug("BetFormat generated: {0}", commandsBuilder.ToString());
			Log.Debug("BetModFormat generated: {0}", commandsModBuilder.ToString());

			BetFormat = commandsBuilder.ToString();
			BetModFormat = commandsModBuilder.ToString();
		}

		public bool NeedMod { get; } = false;

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList == null || e.Command.ArgumentsAsList.Count < 1)
				return CommandHelpers.ChangeOutputIfMod(e.Command.ChatMessage.UserType, BetFormat, BetModFormat);
			var successful = _commands.TryGetValue(e.Command.ArgumentsAsList[0].ToLower(), out var command);
			if (!successful)
				return CommandHelpers.ChangeOutputIfMod(e.Command.ChatMessage.UserType, BetFormat, BetModFormat);
			if ((!command.NeedMod || e.Command.ChatMessage.UserType < UserType.Moderator) && command.NeedMod)
				return null;
			if (command.requireActive && !_betManager.BetActive)
				return StandardMessages.BetMessages.NotActive;
			return await command.processor(e).ConfigureAwait(false);
		}

		private Task<string> CreateBetAsync(OnChatCommandReceivedArgs e)
		{
			var betName = e.Command.ArgumentsAsString.Remove(0, e.Command.ArgumentsAsList[0].Length);
			if(string.IsNullOrEmpty(betName)) return Task.FromResult(StandardMessages.BetMessages.CreateFromat);
			if (_betManager.CreateBet(betName))
				return Task.FromResult($"/me Pariul {_betManager.BetName} a inceput: Poti vota cu XP pe YES(1) sau NO(2)");
			return Task.FromResult("/me Pariu deja activ. Termina sau anuleaza pariul curent.");
		}

		private Task<string> CancelBetAsync(OnChatCommandReceivedArgs e)
		{
			var state = _betManager.CancelBet();
			if (state == BetState.ActionSucceeded)
				return Task.FromResult($"/me Pariul \"{_betManager.BetName}\" este anulat si XP-ul a fost returnat.");
			return Task.FromResult(DefaultMessages(state));
		}

		private async Task<string> EndBetAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count < 2) return StandardMessages.BetMessages.EndBetFormat;
			if (!TryStringToOption(e.Command.ArgumentsAsList[1], out var option))
				return StandardMessages.BetMessages.OptionInvalid;
			var state = await _betManager.EndBetAsync(option).ConfigureAwait(false);
			if (state != BetState.ActionSucceeded) return DefaultMessages(state);
			return $"/me Pariul s-a termiant, a castigat optiunea \"{OptionToString(option)}\". Loz: {_betManager.LatestPrize}xp";
		}

		private async Task<string> MakeVoteAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count < 3) return StandardMessages.BetMessages.MakeVoteFormat;
			if (!int.TryParse(e.Command.ArgumentsAsList[1], out var betPoints))
				return StandardMessages.ErrorMessages.NotNumber;
			if (!TryStringToOption(e.Command.ArgumentsAsList[2], out var option))
				return StandardMessages.BetMessages.OptionInvalid;
			var state = await _betManager.MakeVoteAsync(betPoints, option, e.Command.ChatMessage.UserId)
				.ConfigureAwait(false);
			switch (state)
			{
				case BetState.ActionFailed: return StandardMessages.BetMessages.MinPoints;
				case BetState.ActionSucceeded:
					var vote = _betManager.GetUserVote(e.Command.ChatMessage.UserId);
					return
						$"/me {e.Command.ChatMessage.DisplayName}, ai pariat {vote.points}XP pe {OptionToString(vote.option)} | Bucata detinuta momentan: {Stake(e.Command.ChatMessage.UserId)}";
				default: return DefaultMessages(state);
			}
		}

		private Task<string> UndoBetAsync(OnChatCommandReceivedArgs e)
		{
			var state = _betManager.UndoVote(e.Command.ChatMessage.UserId);
			switch (state)
			{
				case BetState.ActionFailed:
					return Task.FromResult($"/me {e.Command.ChatMessage.DisplayName} nu ai pariat.");
				case BetState.ActionSucceeded:
					return Task.FromResult($"/me {e.Command.ChatMessage.DisplayName}, pariul a fost anulat.");
				default: return Task.FromResult(DefaultMessages(state));
			}
		}

		private Task<string> OpenBetAsync(OnChatCommandReceivedArgs e)
		{
			_betManager.BetOn();
			return Task.FromResult("/me Pariatul a fost deschis!");
		}

		private Task<string> CloseBetAsync(OnChatCommandReceivedArgs e)
		{
			_betManager.BetOff();
			return Task.FromResult("/me Pariatul a fost inchis!");
		}

		private Task<string> StateOfBetAsync(OnChatCommandReceivedArgs e)
		{
			var builder = new StringBuilder("/me Stare:");
			builder.Append(_betManager.BetLocked ? " Inchis" : " Deschis");
			builder.Append(" | Pariuri: ");
			builder.Append(PoolState());
			return Task.FromResult(builder.ToString());
		}

		private string PoolState()
		{
			var restultOne = _betManager.GetOptionAttributes(1);
			var restultTwo = _betManager.GetOptionAttributes(2);
			var builder = new StringBuilder("YES: ");
			builder.AppendFormat("{0} voturi si {1}XP ", restultOne.voters, restultOne.poolSum);
			builder.Append("NO: ");
			builder.AppendFormat("{0} voturi si {1}XP ", restultTwo.voters, restultTwo.poolSum);
			return builder.ToString();
		}

		private async Task<string> CheckUserAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count < 2)
			{
				var vote = _betManager.GetUserVote(e.Command.ChatMessage.UserId);
				if (vote.option == 0)
					return $"/me {e.Command.ChatMessage.DisplayName} nu ai pariat";
				return $"/me {e.Command.ChatMessage.DisplayName} ai pariat {vote.points}XP pe {OptionToString(vote.option)} | Castig Potential: {Math.Round(_betManager.PotentialWin(e.Command.ChatMessage.UserId), 0)}XP Bucata detinuta: {Stake(e.Command.ChatMessage.UserId)}";
			}
			else
			{
				User user;
				try
				{
					user = await _apiRetriever.GetUserByUsernameAsync(e.Command.ArgumentsAsList[1].TrimStart('@'))
						.ConfigureAwait(false);
				}
				catch (BadParameterException exception)
				{
					Log.Error(exception, "Bad parameter {parameter}", e.Command.ArgumentsAsString);
					return string.Format(StandardMessages.ErrorMessages.InvalidName, e.Command.ArgumentsAsList[1]);
				}
				catch (BadRequestException exception)
				{
					Log.Error(exception, "Bad request {parameter}", e.Command.ArgumentsAsString);
					return string.Format(StandardMessages.ErrorMessages.InvalidName, e.Command.ArgumentsAsList[1]);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "WRONG PARAMETER {parameter}", e.Command.ArgumentsAsString);
					return $"/me Unexpected error. Please report! Parameter: \"{e.Command.ArgumentsAsString}\"";
				}
				if(user == null) return string.Format(StandardMessages.ErrorMessages.UserMissing, e.Command.ArgumentsAsList[1].TrimStart('@'));

				var vote = _betManager.GetUserVote(user.Id);
				if (vote.option == 0)
					return $"/me {e.Command.ChatMessage.DisplayName} nu a pariat";
				return $"/me {user.DisplayName} a pariat {vote.points}XP pe {OptionToString(vote.option)}  | Castig Potential: {_betManager.PotentialWin(user.Id)}XP Bucata detinuta: {Stake(user.Id)}";

			}
		}

		private string DefaultMessages(BetState state)
		{
			switch (state)
			{
				case BetState.BetNotActive:
					return StandardMessages.BetMessages.NotActive;
				case BetState.ActionSucceeded:
					return "/me This message should have never showed. Please SEND LOGS and Report.";
				case BetState.ActionFailed:
					return "/me This message should have never showed. Please SEND LOGS and Report.";
				case BetState.ActionError:
					return "/me Action ERRORED. This message should have never showed. Please SEND LOGS and Report.";
				case BetState.BetLocked:
					return StandardMessages.BetMessages.Locked;
				case BetState.NotEnoughPoints:
					return StandardMessages.ErrorMessages.NotEnoughPoints;
				case BetState.OptionInvalid:
					return StandardMessages.BetMessages.OptionInvalid;
			}

			return "This is a new Unhandled Case! Please SEND LOGS.";
		}

		private string Stake(string userId)
		{
			var vote = _betManager.GetUserVote(userId);
			var option = _betManager.GetOptionAttributes(vote.option);
			return $"{Math.Round(vote.points / (double)option.poolSum * 100, 1)}%";
		}

		private string OptionToString(int option)
		{
			if (option == 1) return "YES";
			if (option == 2) return "NO";
			return "INVALID OPTION";
		}

		//1 = YES
		//2 = NO
		private int StringToOption(string optionString)
		{
			var compare = string.Compare(optionString, "yes", StringComparison.InvariantCultureIgnoreCase);
			if (compare == 0) return 1;
			compare = string.Compare(optionString, "no", StringComparison.InvariantCultureIgnoreCase);
			if (compare == 0) return 2;
			return 0;
		}

		private bool TryStringToOption(string option, out int output)
		{
			output = StringToOption(option);
			return output != 0 || int.TryParse(option, out output) && _betManager.IsOptionValid(output);
		}
	}
}
