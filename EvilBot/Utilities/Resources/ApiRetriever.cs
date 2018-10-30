using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.TwitchBot.Interfaces;
using EvilBot.Utilities.Resources.Interfaces;
using Serilog;
using TwitchLib.Api.V5.Models.Users;

namespace EvilBot.Utilities.Resources
{
    public class ApiRetriever : IApiRetriever
    {
        private readonly ITwitchConnections _twitchConnections;

        public ApiRetriever(ITwitchConnections twitchConnections)
        {
            _twitchConnections = twitchConnections;
        }
        public async Task<TimeSpan?> GetUptimeAsync()
        {
            var userId = await GetUserIdAsync(TwitchInfo.ChannelName).ConfigureAwait(false);
            if (userId == null)
            {
                return null;
            }
            return _twitchConnections.Api.V5.Streams.GetUptimeAsync(userId).Result;
        }
        public async Task<User> GetUserAsyncByUsername(string username)
        {
            username = username.Trim('@');
            Log.Debug("AskedForID for {Username}", username);
            User[] userList;
            try
            {
                userList = (await _twitchConnections.Api.V5.Users.GetUserByNameAsync(username).ConfigureAwait(false)).Matches;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetUserAsyncByUsername blew up with {username}", username);
                return null;
            }

            if (userList.Length != 0) return userList[0];
            Log.Warning("User does not exit {username}", username);
            return null;
        }
        public async Task<User> GetUserAsyncById(string userId)
        {
            Log.Debug("AskedForID for {Username}", userId);
            User user;
            try
            {
                user = await _twitchConnections.Api.V5.Users.GetUserByIDAsync(userId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "GetUserAsyncByUsername blew up with {username}", userId);
                return null;
            }

            if (user != null) return user;
            Log.Warning("User does not exit {username}", userId);
            return null;
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
                return null;
            }

            if (userList.Length != 0) return userList[0].Id;
            Log.Warning("User does not exit {username}", username);
            return null;
        }

        public async Task<List<IUserBase>> GetChannelSubscribers(string channelId)
        {
            var subscribers =
                (await _twitchConnections.Api.V5.Channels.GetChannelSubscribersAsync(channelId).ConfigureAwait(false))
                .Subscriptions.ToList();

            return subscribers.Select(t => new UserBase(t.User.DisplayName, t.User.Id)).ToList<IUserBase>();
        }
    }
}