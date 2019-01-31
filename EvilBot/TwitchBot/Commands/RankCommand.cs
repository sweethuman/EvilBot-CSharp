using System;
using System.Globalization;
using System.Threading.Tasks;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using Serilog;
using TwitchLib.Api.Core.Exceptions;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class RankCommand : ITwitchCommand
	{
		private readonly IRankManager _rankManager;
		private readonly IApiRetriever _apiRetriever;
		private readonly IDataAccess _dataAccess;

		public bool NeedMod { get; } = false;

		public RankCommand(IRankManager rankManager, IApiRetriever apiRetriever, IDataAccess dataAccess)
		{
			_rankManager = rankManager;
			_apiRetriever = apiRetriever;
			_dataAccess = dataAccess;
		}

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
			{
				var results = await _dataAccess
					.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, e.Command.ChatMessage.UserId)
					.ConfigureAwait(false);
				var displayName = e.Command.ChatMessage.DisplayName;
				if (results == null) return $"/me {displayName} nu esti inca in baza de date! Vei fi adaugat imediat!";
				var rankFormatted = _rankManager.GetRankFormatted(results.Rank, results.Points);
				var hoursWatched = Math.Round(double.Parse(results.Minutes, CultureInfo.InvariantCulture) / 60, 1)
					.ToString(CultureInfo.CurrentCulture);
				return
					$"/me {displayName} esti {rankFormatted} cu {hoursWatched} ore!";
			}
			else
			{
				User user;
				try
				{
					user = await _apiRetriever.GetUserByUsernameAsync(e.Command.ArgumentsAsList[0].TrimStart('@'))
						.ConfigureAwait(false);
				}
				catch (BadParameterException exception)
				{
					Log.Error(exception, "Bad parameter {parameter}", e.Command.ArgumentsAsString);
					return String.Format(StandardMessages.ErrorMessages.InvalidName, e.Command.ArgumentsAsList[0]);
				}
				catch (BadRequestException exception)
				{
					Log.Error(exception, "Bad request {parameter}", e.Command.ArgumentsAsString);
					return String.Format(StandardMessages.ErrorMessages.InvalidName, e.Command.ArgumentsAsList[0]);
				}
				catch (Exception exception)
				{
					Log.Error(exception, "WRONG PARAMETER {parameter}", e.Command.ArgumentsAsString);
					return $"/me Unexpected error. Please report! Parameter: \"{e.Command.ArgumentsAsString}\"";
				}
				if(user == null) return String.Format(StandardMessages.ErrorMessages.UserMissing, e.Command.ArgumentsAsList[0].TrimStart('@'));

				var results = await _dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, user.Id)
					.ConfigureAwait(false);
				if (results == null) return $"/me {user.DisplayName} nu este inca in baza de date!";

				var rankFormatted = _rankManager.GetRankFormatted(results.Rank, results.Points);
				var hoursWatched = Math.Round(double.Parse(results.Minutes, CultureInfo.InvariantCulture) / 60, 1)
					.ToString(CultureInfo.CurrentCulture);
				return
					$"/me {user.DisplayName} este {rankFormatted} cu {hoursWatched} ore!";
			}
		}
	}
}
