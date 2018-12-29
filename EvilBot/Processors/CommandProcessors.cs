using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Database.Interfaces;
using EvilBot.Managers.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.Utilities;
using Serilog;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Events;

namespace EvilBot.Processors
{
	internal class CommandProcessor : ICommandProcessor
	{
		private readonly IApiRetriever _apiRetriever;
		private readonly IDataAccess _dataAccess;
		private readonly IDataProcessor _dataProcessor;
		private readonly IFilterManager _filterManager;
		private readonly IPollManager _pollManager;

		public CommandProcessor(IDataProcessor dataProcessor, IDataAccess dataAccess, IPollManager pollManager,
			IFilterManager filterManager, IApiRetriever apiRetriever)
		{
			_dataProcessor = dataProcessor;
			_dataAccess = dataAccess;
			_pollManager = pollManager;
			_filterManager = filterManager;
			_apiRetriever = apiRetriever;
			BuildRankListString();
		}

		private string PollOptionsString { get; set; }
		private string RankListString { get; set; }


		public async Task<string> RankCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
			{
				var results = await _dataAccess
					.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, e.Command.ChatMessage.UserId)
					.ConfigureAwait(false);
				var displayName = e.Command.ChatMessage.DisplayName;
				if (results == null) return $"/me {displayName} nu esti inca in baza de date! Vei fi adaugat imediat!";
				var rankFormatted = _dataProcessor.GetRankFormatted(results.Rank, results.Points);
				var hoursWatched = Math.Round(double.Parse(results.Minutes, CultureInfo.InvariantCulture) / 60, 1)
					.ToString(CultureInfo.CurrentCulture);
				return
					$"/me {displayName} esti {rankFormatted} cu {hoursWatched} ore!";
			}
			else
			{
				string userId;
				try
				{
					userId = await _apiRetriever
						.GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@').ToLower()).ConfigureAwait(false);
				}
				catch (BadParameterException exception)
				{
					Log.Error(exception, "Bad parameter {parameter}", e.Command.ArgumentsAsString);
					return StandardMessages.InvalidName(e.Command.ArgumentsAsList[0]);
				}
				catch (BadRequestException exception)
				{
					Log.Error(exception, "Bad request {parameter}", e.Command.ArgumentsAsString);
					return StandardMessages.InvalidName(e.Command.ArgumentsAsList[0]);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "WRONG PARAMETER {parameter}", e.Command.ArgumentsAsString);
					return $"/me Unexpected error. Please report! Parameter: \"{e.Command.ArgumentsAsString}\"";
				}

				var results = await _dataAccess.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, userId)
					.ConfigureAwait(false);
				var displayName = e.Command.ArgumentsAsList[0].TrimStart('@');
				if (results == null) return $"/me {displayName} nu este inca in baza de date!";
				var rankFormatted = _dataProcessor.GetRankFormatted(results.Rank, results.Points);
				var hoursWatched = Math.Round(double.Parse(results.Minutes, CultureInfo.InvariantCulture) / 60, 1)
					.ToString(CultureInfo.CurrentCulture);
				return
					$"/me {displayName} este {rankFormatted} cu {hoursWatched} ore!";
			}
		}

		public async Task<string> ManageCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString)) return StandardMessages.ManageCommandText;
			string userid;
			try
			{
				userid = await _apiRetriever.GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@'))
					.ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Invalid username {username}", e.Command.ArgumentsAsList[0].TrimStart('@'));
				return StandardMessages.InvalidName(e.Command.ArgumentsAsList[0]);
			}

			if (e.Command.ArgumentsAsList.Count < 2 || userid == null)
				return StandardMessages.ManageCommandText;
			var (minuteString, pointsString) = CommandHelpers.ManageCommandSorter(
				e.Command.ArgumentsAsList.ElementAtOrDefault(1), e.Command.ArgumentsAsList.ElementAtOrDefault(2));
			if (!int.TryParse(minuteString ?? "0", out var minuteModifier)) return StandardMessages.ManageCommandText;
			if (!int.TryParse(pointsString ?? "0", out var pointModifier)) return StandardMessages.ManageCommandText;

			await _dataAccess.ModifierUserIdAsync(userid, pointModifier, minuteModifier).ConfigureAwait(false);
			return $"/me Modificat {e.Command.ArgumentsAsList[0]} cu {pointModifier} puncte si {minuteModifier} minute";
		}

		public async Task<string> FilterCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count < 1) return StandardMessages.FilterText;

			User user = null;
			if (e.Command.ArgumentsAsList.Count >= 2)
			{
				try
				{
					user = await _apiRetriever.GetUserByUsernameAsync(e.Command.ArgumentsAsList[1])
						.ConfigureAwait(false);
				}
				catch (Exception exception)
				{
					Log.Error(exception.Message, "Bad request {parameter}", e.Command.ArgumentsAsString);
					return StandardMessages.InvalidName(e.Command.ArgumentsAsList[1]);
				}

				if (user == null) return StandardMessages.UserMissingText(e.Command.ArgumentsAsList[1]);
			}

			switch (e.Command.ArgumentsAsList[0])
			{
				case "get":
				{
					if (user != null)
					{
						if (_filterManager.CheckIfUserIdFiltered(user.Id))
							return $"/me {user.DisplayName} este filtrat!";
						return $"/me {user.DisplayName} nu este filtrat!";
					}

					var filteredUsers = _filterManager.RetrieveFilteredUsers();
					if (filteredUsers.Count <= 0) return "/me Nici un User filtrat!";
					var builder = new StringBuilder();
					builder.Append("Useri filtrati:");
					for (var i = 0; i < filteredUsers.Count; i++) builder.Append($" {filteredUsers[i].DisplayName},");
					return $"/me {builder}";
				}
				case "add":
				{
					if (user == null) return StandardMessages.FilterText;
					if (await _filterManager.AddToFilterAsync(new UserBase(user.DisplayName, user.Id))
						.ConfigureAwait(false))
						return $"/me {user.DisplayName} adaugat la Filtru!";
					return $"/me {user.DisplayName} deja in Filtru!";
				}
				case "rem":
				case "remove":
				{
					if (user == null) return StandardMessages.FilterText;
					if (await _filterManager.RemoveFromFilterAsync(new UserBase(user.DisplayName, user.Id))
						.ConfigureAwait(false))
						return $"/me {user.DisplayName} sters din Filtru!";
					return $"/me {user.DisplayName} nu este in Filtru!";
				}
				default:
					return StandardMessages.FilterText;
			}
		}

		public string RanksListCommand(OnChatCommandReceivedArgs e)
		{
			return $"/me {RankListString}";
		}

		public async Task<string> TopCommandAsync(OnChatCommandReceivedArgs e)
		{
			Log.Debug("Top Command Started!");
			var databaseUsers = await _dataAccess.RetrieveNumberOfUsersFromTableAsync(Enums.DatabaseTables.UserPoints,
				6,
				Enums.DatabaseUserPointsOrderRow.Points).ConfigureAwait(false);
			if (databaseUsers == null) return "/me Baza de date este goala!";
			databaseUsers.RemoveAll(x => x.UserId == _apiRetriever.TwitchChannelId);
			if (databaseUsers.Count < 1) return "/me Nu am ce afisa!";
			var userIdList = databaseUsers.Select(t => t.UserId).ToList();
			List<TwitchLib.Api.Helix.Models.Users.User> twitchUsers;
			try
			{
				twitchUsers = await _apiRetriever.GetUsersHelixAsync(userIdList).ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "TopCommand Failed");
				return "/me TopCommand a esuat sa obtina userii. Te rog trimite LOGurile.";
			}

			if (twitchUsers == null || twitchUsers.Count == 0)
			{
				var builder1 = new StringBuilder();
				for (var i = 0; i < databaseUsers.Count; i++)
					builder1.AppendFormat("{0}, ", databaseUsers[i].UserId);
				Log.Error("No data could be obtained from twitch servers wih {ids}", builder1);
				return "/me ERROR. NU SE POT OBTINE NICI O DATA A NICI UNUI USER. SEND LOGS.";
			}

			var query =
				from databaseUser in databaseUsers
				join twitchUser in twitchUsers on databaseUser.UserId equals twitchUser.Id
				orderby int.Parse(databaseUser.Points) descending
				select new UserStructureData(twitchUser.DisplayName, databaseUser.Id, twitchUser.Id,
					databaseUser.Points, databaseUser.Minutes, databaseUser.Rank);
			var userList = query.ToList();
			var builder = new StringBuilder();
			builder.Append("Top: ");
			for (var i = 0; i < userList.Count && i < 5; i++)
				builder.AppendFormat("{0}.{1}(Lvl. {2}):{3}xp ", i + 1, userList[i].DisplayName,
					userList[i].Rank, userList[i].Points);
			Log.Debug("Top Command finished successfully!");
			return $"/me {builder}";
		}

		public async Task<(string usersAnnouncement, string winnerAnnouncement)> GiveawayCommandAsync(
			OnChatCommandReceivedArgs e)
		{
			Log.Information("Giveaway started!");
			try
			{
				var userList = await _apiRetriever.GetChattersUsersAsync(TwitchInfo.ChannelName).ConfigureAwait(false);
				userList.RemoveAll(x =>
					string.Equals(x.Name, TwitchInfo.ChannelName, StringComparison.CurrentCultureIgnoreCase));
				var getDatabaseUsersTasks = new List<Task<IDatabaseUser>>();
				for (var i = 0; i < userList.Count; i++)
				{
					if (!_filterManager.CheckIfUserIdFiltered(userList[i].Id))
					{
						getDatabaseUsersTasks.Add(
							_dataAccess.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, userList[i].Id));
						continue;
					}

					userList.RemoveAll(x => x.Id == userList[i].Id);
					i--;
				}

				var databaseUsers = (await Task.WhenAll(getDatabaseUsersTasks).ConfigureAwait(false)).ToList();
				databaseUsers.RemoveAll(x => x == null);

				var query =
					from databaseUser in databaseUsers
					join user in userList on databaseUser.UserId equals user.Id
					where int.Parse(databaseUser.Rank) >= 2
					select new UserStructureData(user.DisplayName, databaseUser.Id, user.Id, databaseUser.Points,
						databaseUser.Minutes, databaseUser.Rank);
				var sourceAccounts = query.ToList();
				if (sourceAccounts.Count < 1)
					return (null, "/me Nu exista oameni eligibili pentru giveaway");

				var randomNumber = new Random()
					.Next(0, sourceAccounts.Count);
				var winner =
					$"/me {sourceAccounts[randomNumber].DisplayName} a castigat {e.Command.ArgumentsAsString}!";
				var builder = new StringBuilder();
				builder.Append("/me Inscrisi: ");
				for (var i = 0; i < sourceAccounts.Count; i++)
					builder.AppendFormat("{0}, ", sourceAccounts[i].DisplayName);

				Log.Information("Giveaway ran with success!");
				return (builder.ToString(), winner);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "GiveawayCommand failed!");
				return (null, "/me SORRY, GIVEAWAY FAILED, PLEASE TRY AGAIN LATER, also send logs, thx");
			}
		}

		private void BuildRankListString()
		{
			var rankList = _dataProcessor.GetRankList();
			var builder = new StringBuilder();
			for (var i = 1; i < rankList.Count; i++)
				builder.AppendFormat("{0}.{1}:{2} ", rankList[i].Id, rankList[i].Name, rankList[i].RequiredPoints);
			RankListString = builder.ToString();
		}

		#region PollCommands

		public string PollCreateCommand(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString) || e.Command.ArgumentsAsString.Contains("||"))
				return StandardMessages.PollCreateText;

			var options = CommandHelpers.FilterAndPreparePollOptions(e.Command.ArgumentsAsString);
			if (options.Count < 2) return StandardMessages.PollCreateText;

			var creationSuccess = _pollManager.PollCreate(options);
			if (!creationSuccess)
			{
				Log.Error("Something major failed when creating the poll {paramString}", e.Command.ArgumentsAsString);
				return StandardMessages.BigError;
			}

			var resultItems = _pollManager.PollStats();
			var builder = new StringBuilder();
			builder.Append("Poll Creat! Optiuni: ");
			for (var i = 0; i < resultItems.Count; i++) builder.AppendFormat(" //{0}:{1}", i + 1, resultItems[i].Name);
			PollOptionsString = CommandHelpers.OptionsStringBuilder(_pollManager.PollStats().Count);
			return $"/me {builder}";
		}

		public async Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (!int.TryParse(e.Command.ArgumentsAsString, out var votedNumber))
				return StandardMessages.PollVoteNotNumber;
			var voteState = await _pollManager.PollAddVoteAsync(e.Command.ChatMessage.UserId, votedNumber)
				.ConfigureAwait(false);

			switch (voteState)
			{
				case Enums.PollAddVoteFinishState.PollNotActive:
					return StandardMessages.PollNotActiveText;
				case Enums.PollAddVoteFinishState.VoteAdded:
					return
						$"/me {e.Command.ChatMessage.DisplayName} a votat pentru '{_pollManager.PollItems[votedNumber - 1].Name}'";
				case Enums.PollAddVoteFinishState.OptionOutOfRange:
					if (PollOptionsString != null) return $"/me Foloseste !pollvote {PollOptionsString}";
					Log.Error("PollOptionsString shouldn't be null when vote is out of range... returning null!");
					return "/me Foloseste !pollvote ERROR: LIPSESC OPTIUNILE. SEND LOGS.";
				case Enums.PollAddVoteFinishState.VoteFailed:
					Log.Error("Vote failer for {DisplayName}, chat message: {message}",
						e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
					return
						$"/me {e.Command.ChatMessage.DisplayName} votul tau a esuat. Te rog contacteaza un moderator!";
				default:
					return null;
			}
		}

		public string PollStatsCommand(OnChatCommandReceivedArgs e)
		{
			var resultItems = _pollManager.PollStats();
			if (resultItems == null)
				return StandardMessages.PollNotActiveText;

			var builder = new StringBuilder();
			builder.Append("Statistici :");
			for (var i = 0; i < resultItems.Count; i++)
				builder.AppendFormat(" //{0}:{1}", resultItems[i].Name, resultItems[i].Points);
			return $"/me {builder}";
		}

		public string PollEndCommand(OnChatCommandReceivedArgs e)
		{
			var resultItem = _pollManager.PollEnd();
			if (resultItem == null)
				return StandardMessages.PollNotActiveText;
			PollOptionsString = null;
			var message = $"A Castigat || {resultItem.Name} || cu {resultItem.Points} puncte";
			return $"/me {message}";
		}

		#endregion PollCommands
	}
}