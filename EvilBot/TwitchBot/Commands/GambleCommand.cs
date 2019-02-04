using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.Resources;
using EvilBot.Resources.Enums;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using Serilog;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class GambleCommand : ITwitchCommand
	{

		private readonly IDataAccess _dataAccess;

		public GambleCommand(IDataAccess dataAccess)
		{
			_dataAccess = dataAccess;
		}

		public bool NeedMod { get; } = false;

		private readonly Dictionary<string, int> _lostGambles = new Dictionary<string, int>();
		private readonly Random _random = new Random();

		private string GambleFormat { get; } = "/me !gamble <points>";

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
				return GambleFormat;

			if (!int.TryParse(e.Command.ArgumentsAsList[0], out var points))
				return StandardMessages.ErrorMessages.NotNumber;

			var databaseUser =
				await _dataAccess.RetrieveUserFromTableAsync(DatabaseTables.UserPoints, e.Command.ChatMessage.UserId).ConfigureAwait(false);

			if (databaseUser == null)
				return string.Format(StandardMessages.ErrorMessages.NotInDatabase, e.Command.ChatMessage.DisplayName);

			if (!int.TryParse(databaseUser.Points, out var userPoints))
			{
				Log.Error("COULDN'T parse databse poitns {points}", databaseUser.Points);
				return StandardMessages.ErrorMessages.BigError;
			}

			if (userPoints < points)
				return StandardMessages.ErrorMessages.NotEnoughPoints;

			if (!_lostGambles.TryGetValue(e.Command.ChatMessage.UserId, out var tries))
			{
				_lostGambles[e.Command.ChatMessage.UserId] = 0;
				tries = 0;
			}

			if (_random.Next(0, 100) <= (tries < 2 ? 40 : 60))
			{
				_lostGambles[e.Command.ChatMessage.UserId] = 0;
				await _dataAccess.ModifierUserIdAsync(e.Command.ChatMessage.UserId, points).ConfigureAwait(false);
				return $"/me {e.Command.ChatMessage.DisplayName} ai castigat : {points * 2}XP ";
			}


			_lostGambles[e.Command.ChatMessage.UserId] = tries + 1;
			await _dataAccess.ModifierUserIdAsync(e.Command.ChatMessage.UserId, -1 * points).ConfigureAwait(false);
			return $"/me {e.Command.ChatMessage.DisplayName} ai pierdut : {points}XP";
		}
	}
}
