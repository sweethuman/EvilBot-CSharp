using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
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

		public CommandProcessor(IDataProcessor dataProcessor, IDataAccess dataAccess, IPollManager pollManager,
			IFilterManager filterManager, IApiRetriever apiRetriever)
		{
			_dataProcessor = dataProcessor;
			_dataAccess = dataAccess;
			_pollManager = pollManager;
			_filterManager = filterManager;
			_apiRetriever = apiRetriever;
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
				return _filterManager.RetrieveFilteredUsers();
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
			return $"/me {builder}";
		}

		public async Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (!int.TryParse(e.Command.ArgumentsAsString, out var votedNumber)) return StandardMessages.PollVoteText;
			var voteState = await _pollManager.PollAddVote(e.Command.ChatMessage.UserId, votedNumber)
				.ConfigureAwait(false);

			switch (voteState)
			{
				case Enums.PollAddVoteFinishState.PollNotActive:
					return StandardMessages.PollNotActiveText;
				case Enums.PollAddVoteFinishState.VoteAdded:
					return
						$"/me {e.Command.ChatMessage.DisplayName} a votat pentru {_pollManager.PollItems[votedNumber - 1]}";
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

			var message = $"A Castigat || {resultItem.Item} || cu {resultItem.ItemPoints} puncte";
			return $"/me {message}";
		}

		#endregion PollCommands
	}
}