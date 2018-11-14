using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.V5.Models.Users;

namespace EvilBot.Utilities.Resources.Interfaces
{
	public interface IApiRetriever
	{
		Task<TimeSpan?> GetUptimeAsync();
		Task<User> GetUserAsyncByUsername(string username);
		Task<User> GetUserAsyncById(string userId);
		Task<string> GetUserIdAsync(string username);
		Task<List<IUserBase>> GetChannelSubscribers(string channelId);
		Task<List<IUser>> GetChatterUsers(string channelName);
		string TwitchChannelId { get; }
	}
}