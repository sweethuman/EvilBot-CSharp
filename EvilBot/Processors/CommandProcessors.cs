using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Database.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;
using EvilBot.Utilities.Resources;
using EvilBot.Utilities.Resources.Interfaces;
using Serilog;
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
		
		private string PollOptionsString { get; set; }
		private string RankListString { get; set; }

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

		private void BuildRankListString()
		{
			var rankList = _dataProcessor.GetRankList();
			var builder = new StringBuilder();
			for (int i = 1; i < rankList.Count; i++)
				builder.AppendFormat("{0}.{1}:{2} ", rankList[i].Id, rankList[i].Name, rankList[i].RequiredPoints);
			RankListString = builder.ToString();
		}
		
		public async Task<string> RankCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
			{
				var results = await _dataProcessor.GetUserAttributesAsync(e.Command.ChatMessage.UserId)
					.ConfigureAwait(false);
				if (results != null)
					return
						$"/me {e.Command.ChatMessage.DisplayName} esti {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], CultureInfo.InvariantCulture) / 60, 1)} ore!\n\r";
				return
					$"/me {e.Command.ChatMessage.DisplayName} Nu esti in baza de date! Vei fi adaugat la urmatorul check!";
			}
			else
			{
				var results = await _dataProcessor
					.GetUserAttributesAsync(await _apiRetriever
						.GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false))
					.ConfigureAwait(false);
				if (results != null)
					return
						$"/me {e.Command.ArgumentsAsString.TrimStart('@')} este {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], CultureInfo.InvariantCulture) / 60, 1)} ore!";
				return $"/me {e.Command.ArgumentsAsString.TrimStart('@')} nu este inca in baza de date!";
			}
		}

		public async Task<string> ManageCommandAsync(OnChatCommandReceivedArgs e)
		{
			string userid;
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString)) return StandardMessages.ManageCommandText;
			if (e.Command.ArgumentsAsList.Count < 2 || (userid = await _apiRetriever
				    .GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@')).ConfigureAwait(false)) == null)
				return StandardMessages.ManageCommandText;
			var pointModifier = 0;
			var minuteModifier = 0;
			var twoParams = false;
			var parameters = new List<string> {e.Command.ArgumentsAsList[1]};
			if (e.Command.ArgumentsAsList.Count == 3)
			{
				twoParams = true;
				parameters =
					CommandHelpers.ManageCommandSorter(e.Command.ArgumentsAsList[1], e.Command.ArgumentsAsList[2]);
			}

			if (parameters[0].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
			{
				parameters[0] = parameters[0].TrimEnd('m', 'M');

				if (!int.TryParse(parameters[0], out minuteModifier)) return StandardMessages.ManageCommandText;
			}
			else
			{
				if (!int.TryParse(parameters[0], out pointModifier)) return StandardMessages.ManageCommandText;
			}

			if (twoParams)
			{
				if (parameters[1].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
				{
					parameters[1] = parameters[1].TrimEnd('m', 'M');

					if (!int.TryParse(parameters[1], out minuteModifier)) return StandardMessages.ManageCommandText;
				}
				else
				{
					return StandardMessages.ManageCommandText;
				}
			}

			await _dataAccess.ModifierUserIdAsync(userid, pointModifier, minuteModifier).ConfigureAwait(false);
			return $"/me Modificat {e.Command.ArgumentsAsList[0]} cu {pointModifier} puncte si {minuteModifier} minute";
		}

		public async Task<string> FilterCommand(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count >= 1 && e.Command.ArgumentsAsList[0] == "get")
			{
				var filteredUsers = _filterManager.RetrieveFilteredUsers();
				if (filteredUsers.Count <= 0) return "/me Nici un User filtrat!";
				var builder = new StringBuilder();
				builder.Append("Useri filtrati:");
				for (var i = 0; i < filteredUsers.Count; i++) builder.Append($" {filteredUsers[i].DisplayName},");
				return $"/me {builder}";
			}
			if (e.Command.ArgumentsAsList.Count < 2) return StandardMessages.FilterText;
			switch (e.Command.ArgumentsAsList[0])
			{
				case "add":
				{
					var user = await _apiRetriever.GetUserAsyncByUsername(e.Command.ArgumentsAsList[1]);
					if (user == null) return StandardMessages.UserMissingText;
					if (await _filterManager.AddToFiler(new UserBase(user.DisplayName, user.Id.Trim())))
						return $"/me {user.DisplayName} adaugat la Filtru!";
					return $"/me {user.DisplayName} deja in Filtru!";
				}
				case "remove":
				{
					var user = await _apiRetriever.GetUserAsyncByUsername(e.Command.ArgumentsAsList[1]);
					if (user == null) return StandardMessages.UserMissingText;
					if (await _filterManager.RemoveFromFilter(new UserBase(user.DisplayName, user.Id)))
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

		public async Task<string> TopCommand(OnChatCommandReceivedArgs e)
		{
			var result = await _dataAccess.RetrieveNumberOfUsersFromTable(Enums.DatabaseTables.UserPoints, 6, Enums.DatabaseUserPointsOrderRow.Points);
			if (result == null) return "/me Baza de date este goala!";
			result.RemoveAll(x => x.UserId == _apiRetriever.TwitchChannelId);
			if (result.Count < 1) return "/me Nu am ce afisa!";
			var getUserListTasks = result.Select(t => _apiRetriever.GetUserAsyncById(t.UserId)).ToList();
			var retrievedUserList = (await Task.WhenAll(getUserListTasks)).ToList();
			var builder = new StringBuilder();
			builder.Append("Top: ");
			for (var i = 0; i < retrievedUserList.Count && i < 5; i++)
			{
				builder.AppendFormat("{0}.{1}(Lvl. {2}):{3}p ", i+1, retrievedUserList[i].DisplayName,
					result[i].Rank, result[i].Points);
			}

			return $"/me {builder}";
		}

		public async Task<(string usersAnnouncement, string winnerAnnouncement)> GiveawayCommand(OnChatCommandReceivedArgs e)
		{
			Log.Information("Giveaway started!");
			try
			{
				var userList = await _apiRetriever.GetChatterUsers(TwitchInfo.ChannelName);
				userList.RemoveAll(x => x.Name == TwitchInfo.ChannelName.ToLower());
				var getDatabaseUsersTasks = new List<Task<IDatabaseUser>>();
				for (var i = 0; i < userList.Count; i++)
				{
					if (!_filterManager.CheckIfUserFiltered(new UserBase(userList[i].DisplayName, userList[i].Id)))
					{
						getDatabaseUsersTasks.Add(_dataAccess.RetrieveUserFromTable(Enums.DatabaseTables.UserPoints, userList[i].Id));
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
					return (null,"/me Nu exista oameni eligibili pentru giveaway");
				
				var randomNumber = new Random()
					.Next(0, sourceAccounts.Count);
				var winner =  $"/me {sourceAccounts[randomNumber].DisplayName} a castigat {e.Command.ArgumentsAsString}!";
				var builder = new StringBuilder();
				builder.Append("/me Inscrisi: ");
				for (var i = 0; i < sourceAccounts.Count; i++)
				{
					builder.AppendFormat("{0}, ", sourceAccounts[i].DisplayName);
				}
				
				Log.Information("Giveaway ran with success!");
				return (builder.ToString(), winner);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "GiveawayCommand failed!");
				return (null,"/me SORRY, GIVEAWAY FAILED, PLEASE TRY AGAIN LATER, also send logs, thx");
			}
		}
		
		#region PollCommands

		public string PollCreateCommand(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString) || e.Command.ArgumentsAsString.Contains("||"))
				return StandardMessages.PollCreateText;

			var options = CommandHelpers.FilterAndPreparePollOptions(e.Command.ArgumentsAsString);
			if (options.Count < 2) return StandardMessages.PollCreateText;

			var resultItems = _pollManager.PollCreate(options);
			if (resultItems == null)
			{
				Log.Error("Something major failed when creating the poll {paramString}", e.Command.ArgumentsAsString);
				return StandardMessages.BigError;
			}

			var builder = new StringBuilder();
			builder.Append("Poll Creat! Optiuni: ");
			for (var i = 0; i < resultItems.Count; i++) builder.AppendFormat(" //{0}:{1}", i + 1, resultItems[i]);
			//NOTE FUCC, THIS IS MAY NOT BE STABLE IN FUTURE
			PollOptionsString = CommandHelpers.PollOptionsStringBuilder(_pollManager.PollStats());
			return $"/me {builder}";
		}

		public async Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (!int.TryParse(e.Command.ArgumentsAsString, out var votedNumber)) return StandardMessages.PollVoteNotNumber;
			var voteState = await _pollManager.PollAddVote(e.Command.ChatMessage.UserId, votedNumber)
				.ConfigureAwait(false);

			switch (voteState)
			{
				case Enums.PollAddVoteFinishState.PollNotActive:
					return StandardMessages.PollNotActiveText;
				case Enums.PollAddVoteFinishState.VoteAdded:
					return
						$"/me {e.Command.ChatMessage.DisplayName} a votat pentru {_pollManager.PollItems[votedNumber - 1]}";
				case Enums.PollAddVoteFinishState.OptionOutOfRange:
					if (PollOptionsString != null) return $"/me Foloseste !pollvote {PollOptionsString}";
					Log.Error("PollOptionsString shouldn't be null when vote is out of range... returning null!");
					return null;
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
			for (var i = 0; i < resultItems.Count; i++) builder.AppendFormat(" //{0}:{1}", resultItems[i].Item, resultItems[i].ItemPoints);
			return $"/me {builder}";
		}

		public string PollEndCommand(OnChatCommandReceivedArgs e)
		{
			var resultItem = _pollManager.PollEnd();
			if (resultItem == null)
				return StandardMessages.PollNotActiveText;
			PollOptionsString = null;
			var message = $"A Castigat || {resultItem.Item} || cu {resultItem.ItemPoints} puncte";
			return $"/me {message}";
		}

		#endregion PollCommands
	}
}