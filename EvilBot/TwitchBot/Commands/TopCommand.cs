using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using Serilog;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class TopCommand : ITwitchCommand
	{
		private readonly IDataAccess _dataAccess;
		private readonly IApiRetriever _apiRetriever;

		public TopCommand(IDataAccess dataAccess, IApiRetriever apiRetriever)
		{
			_dataAccess = dataAccess;
			_apiRetriever = apiRetriever;
		}

		public bool NeedMod { get; } = false;

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			Log.Debug("Top Command Started!");
			var databaseUsers = await _dataAccess.RetrieveNumberOfUsersFromTableAsync(DatabaseTables.UserPoints,
				6,
				DatabaseUserPointsOrderRow.Points).ConfigureAwait(false);
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
	}
}
