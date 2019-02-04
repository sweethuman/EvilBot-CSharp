using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Database.Interfaces;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class GiveawayCommand : ITwitchCommand
	{
		private readonly IApiRetriever _apiRetriever;
		private readonly IDataAccess _dataAccess;
		private readonly IConfiguration _configuration;
		private readonly IFilterManager _filterManager;
		private readonly ITwitchConnections _twitchConnection;
		private readonly Random _random = new Random();

		public GiveawayCommand(IFilterManager filterManager, IConfiguration configuration, IDataAccess dataAccess,
			IApiRetriever apiRetriever, ITwitchConnections twitchConnection)
		{
			_filterManager = filterManager;
			_configuration = configuration;
			_dataAccess = dataAccess;
			_apiRetriever = apiRetriever;
			_twitchConnection = twitchConnection;
		}

		public bool NeedMod { get; } = true;

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
				return "/me Nu ai introdus nici un item pentru giveaway!";
			Log.Information("Giveaway started!");
			try
			{
				var userList = await _apiRetriever.GetChattersUsersAsync(_configuration.ChannelName).ConfigureAwait(false);
				userList.RemoveAll(x =>
					string.Equals(x.Name, _configuration.ChannelName, StringComparison.CurrentCultureIgnoreCase));
				var getDatabaseUsersTasks = new List<Task<IDatabaseUser>>();
				for (var i = 0; i < userList.Count; i++)
				{
					if (!_filterManager.CheckIfUserIdFiltered(userList[i].Id))
					{
						getDatabaseUsersTasks.Add(
							_dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, userList[i].Id));
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
					return "/me Nu exista oameni eligibili pentru giveaway";

				var randomNumber = _random.Next(0, sourceAccounts.Count);
				var winner =
					$"/me {sourceAccounts[randomNumber].DisplayName} a castigat {e.Command.ArgumentsAsString}!";
				var builder = new StringBuilder();
				builder.Append("/me Inscrisi: ");
				foreach (var user in sourceAccounts)
					builder.AppendFormat("{0}, ", user.DisplayName);

				Log.Information("Giveaway ran with success!");
				_twitchConnection.Client.SendMessage(e.Command.ChatMessage.Channel, builder.ToString());
				return winner;
			}
			catch (Exception exception)
			{
				Log.Error(exception, "GiveawayCommand failed!");
				return "/me SORRY, GIVEAWAY FAILED, PLEASE TRY AGAIN LATER, also send logs, thx";
			}
		}
	}
}
