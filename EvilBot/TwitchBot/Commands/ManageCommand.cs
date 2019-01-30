using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.Resources;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using EvilBot.Utilities;
using Serilog;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class ManageCommand : ITwitchCommand
	{

		private readonly IApiRetriever _apiRetriever;
		private readonly IDataAccess _dataAccess;

		public ManageCommand(IDataAccess dataAccess, IApiRetriever apiRetriever)
		{
			_dataAccess = dataAccess;
			_apiRetriever = apiRetriever;
		}

		public bool NeedMod { get; } = true;

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count < 2)
				return StandardMessages.ManageCommandText;

			User user;
			try
			{
				user = await _apiRetriever.GetUserByUsernameAsync(e.Command.ArgumentsAsList[0].TrimStart('@'))
					.ConfigureAwait(false);
			}
			catch (Exception exception)
			{
				Log.Error(exception, "Invalid username {username}", e.Command.ArgumentsAsList[0].TrimStart('@'));
				return string.Format(StandardMessages.UserErrorMessages.InvalidName, e.Command.ArgumentsAsList[0]);
			}

			if (user == null)
				return string.Format(StandardMessages.UserErrorMessages.UserMissingText, e.Command.ArgumentsAsList[0].TrimStart('@'));

			var (minuteString, pointsString) = CommandHelpers.ManageCommandSorter(
				e.Command.ArgumentsAsList.ElementAtOrDefault(1), e.Command.ArgumentsAsList.ElementAtOrDefault(2));
			if (!int.TryParse(minuteString ?? "0", out var minuteModifier)) return StandardMessages.ManageCommandText;
			if (!int.TryParse(pointsString ?? "0", out var pointModifier)) return StandardMessages.ManageCommandText;

			await _dataAccess.ModifierUserIdAsync(user.Id, pointModifier, minuteModifier).ConfigureAwait(false);
			var results = await _dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, user.Id)
				.ConfigureAwait(false);
			var hoursWatched = Math.Round(double.Parse(results.Minutes, CultureInfo.InvariantCulture) / 60, 1);
			return $"/me Modificat {user.DisplayName} cu {pointModifier} puncte si {minuteModifier} minute. Acum are {results.Points}xp si {hoursWatched}h";
		}
	}
}
