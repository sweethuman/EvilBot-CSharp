using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Processors;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using Serilog;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Helix.Models.Users;
using User = TwitchLib.Api.V5.Models.Users.User;

namespace EvilBot.Resources
{
	public class ApiRetriever : IApiRetriever
	{
		private readonly ITwitchConnections _twitchConnections;

		public ApiRetriever(ITwitchConnections twitchConnections)
		{
			_twitchConnections = twitchConnections;
			var twitchChannelId = GetUserIdAsync(TwitchInfo.ChannelName).Result;
			TwitchChannelId = twitchChannelId ?? throw new Exception(
				                  "TwitchChannelId is null. Check if channel name is correct or connexions are made correctly");
		}

		public string TwitchChannelId { get; }

		public async Task<User> GetUserByUsernameAsync(string username)
		{
			username = username.Trim('@');
			Log.Debug("AskedForID for {Username}", username);
			User[] userList;
			try
			{
				userList = (await _twitchConnections.Api.V5.Users.GetUserByNameAsync(username).ConfigureAwait(false))
					.Matches;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "GetUserByUsernameAsync blew up with {username}", username);
				throw;
			}

			if (userList.Length != 0) return userList[0];
			Log.Warning("User does not exit {username}", username);
			return null;
		}

		public async Task<User> GetUserByIdAsync(string userId)
		{
			Log.Debug("AskedForID for {Username}", userId);
			User user;
			try
			{
				user = await _twitchConnections.Api.V5.Users.GetUserByIDAsync(userId).ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "GetUserByUsernameAsync blew up with {username}", userId);
				throw;
			}

			if (user != null) return user;
			Log.Warning("User does not exit {username}", userId);
			return null;
		}

		public async Task<List<User>> GetUsersByUsernameAsync(List<string> usernames)
		{
			if (usernames == null || usernames.Count == 0)
				return null;
			usernames = usernames.Select(x => { return x.Trim('@'); }).ToList();

			var builder = new StringBuilder();
			for (var i = 0; i < usernames.Count; i++)
				builder.AppendFormat("{0}, ", usernames[i]);
			Log.Debug("AskedForUser for {usernames}", builder);

			var userList = new List<User>();
			try
			{
				var getUsersTasks = DataProcessor.SplitList(usernames)
					.Select(split => _twitchConnections.Api.V5.Users.GetUsersByNameAsync(split));
				var splitUsersList =
					(await Task.WhenAll(getUsersTasks).ConfigureAwait(false)).Select(t => t.Matches);
				foreach (var splitUser in splitUsersList) userList.AddRange(splitUser);
				if (userList.Count != 0) return userList;
				Log.Warning("None of the users existed {usernames}", builder);
				return null;
			}
			catch (Exception e)
			{
				Log.Warning(e, "BLEW UP WITH {usernames}", builder);
				throw;
			}
		}

		public async Task<string> GetUserIdAsync(string username)
		{
			Log.Debug("AskedForID for {Username}", username);
			User[] userList;
			try
			{
				userList = (await _twitchConnections.Api.V5.Users.GetUserByNameAsync(username).ConfigureAwait(false))
					.Matches;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "GetUserIdAsync blew up with {username}", username);
				throw;
			}

			if (userList.Length != 0) return userList[0].Id;
			Log.Warning("User does not exit {username}", username);
			return null;
		}

		public async Task<List<TwitchLib.Api.Helix.Models.Users.User>> GetUsersHelixAsync
			(List<string> ids = null, List<string> logins = null)
		{
			if ((ids == null || ids.Count == 0) && (logins == null || logins.Count == 0))
				return null;
			if (ids == null) ids = new List<string>();
			if (logins == null) logins = new List<string>();
			var builder = new StringBuilder();
			for (var i = 0; i < ids.Count; i++)
				builder.AppendFormat("{0}, ", ids[i]);
			for (var i = 0; i < logins.Count; i++)
				builder.AppendFormat("{0}, ", logins[i]);
			Log.Debug("Asked for Users of {identifiers}", builder);

			try
			{
				var splitIds = DataProcessor.SplitList(ids).ToList();
				var splitLogins = DataProcessor.SplitList(logins).ToList();
				var minDistance = Math.Min(splitIds.Count, splitLogins.Count);
				var getUsersTasks = new List<Task<GetUsersResponse>>();
				int i;
				for (i = 0; i < minDistance; i++)
					getUsersTasks.Add(_twitchConnections.Api.Helix.Users.GetUsersAsync(splitIds[i], splitLogins[i]));
				for (var j = i; j < splitIds.Count; j++)
					getUsersTasks.Add(_twitchConnections.Api.Helix.Users.GetUsersAsync(splitIds[i]));
				for (var j = i; j < splitLogins.Count; j++)
					getUsersTasks.Add(_twitchConnections.Api.Helix.Users.GetUsersAsync(logins: splitLogins[i]));

				var splitUsers = (await Task.WhenAll(getUsersTasks).ConfigureAwait(false)).Select(t => t.Users);
				var userList = new List<TwitchLib.Api.Helix.Models.Users.User>();
				foreach (var splitUser in splitUsers) userList.AddRange(splitUser);
				return userList.Count == 0 ? null : userList;
			}
			catch (Exception e)
			{
				Log.Error(e, "GetUsersHelixAsync blew up with {usernames}", builder);
				throw;
			}
		}

		public async Task<List<IUserBase>> GetChannelSubscribersAsync(string channelId)
		{
			var subscribers =
				(await _twitchConnections.Api.V5.Channels.GetChannelSubscribersAsync(channelId).ConfigureAwait(false))
				.Subscriptions.ToList();

			return subscribers.Select(t => new UserBase(t.User.DisplayName, t.User.Id)).ToList<IUserBase>();
		}

		public async Task<List<IUser>> GetChattersUsersAsync(string channelName)
		{
			try
			{
				var chatusers = await _twitchConnections.Api.Undocumented.GetChattersAsync(channelName)
					.ConfigureAwait(false);
				var usernamesList = chatusers.Select(t => t.Username).ToList();
				var userList = await GetUsersByUsernameAsync(usernamesList).ConfigureAwait(false);
				if (userList == null) return null;
				userList.RemoveAll(x => x == null);
				return userList.ToList<IUser>();
			}
			catch (Exception e)
			{
				Log.Error(e, "GetChattersUsersAsync Failed, channel: {channel}", channelName);
				throw;
			}
		}
	}
}