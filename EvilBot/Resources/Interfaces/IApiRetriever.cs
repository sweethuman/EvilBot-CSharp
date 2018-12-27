using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.V5.Models.Users;

namespace EvilBot.Resources.Interfaces
{
	public interface IApiRetriever
	{
		string TwitchChannelId { get; }

		Task<User> GetUserByUsernameAsync(string username);
		
		Task<User> GetUserByIdAsync(string userId);
		
		Task<string> GetUserIdAsync(string username);
		
		Task<List<IUserBase>> GetChannelSubscribersAsync(string channelId);
		
		Task<List<IUser>> GetChattersUsersAsync(string channelName);
		
		Task<List<User>> GetUsersByUsernameAsync(List<string> usernames);

		Task<List<TwitchLib.Api.Helix.Models.Users.User>> GetUsersHelixAsync
			(List<string> ids = null, List<string> logins = null);
	}
}