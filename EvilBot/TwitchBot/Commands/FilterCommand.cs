using System;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using Serilog;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class FilterCommand : ITwitchCommand
	{

		private readonly IApiRetriever _apiRetriever;
		private readonly IFilterManager _filterManager;

		public FilterCommand(IApiRetriever apiRetriever, IFilterManager filterManager)
		{
			_apiRetriever = apiRetriever;
			_filterManager = filterManager;
		}

		public bool NeedMod { get; } = true;

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
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
					return String.Format(StandardMessages.UserErrorMessages.InvalidName, e.Command.ArgumentsAsList[1]);
				}

				if (user == null) return String.Format(StandardMessages.UserErrorMessages.UserMissingText, e.Command.ArgumentsAsList[1]);
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
	}
}
