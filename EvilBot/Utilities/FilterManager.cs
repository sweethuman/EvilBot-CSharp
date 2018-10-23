using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Processors.Interfaces;
using EvilBot.Utilities.Interfaces;
using Serilog;
using TwitchLib.Api.V5.Models.Users;

namespace EvilBot.Utilities
{
    public class FilterManager : IFilterManager
    {
        //TODO add somewhere code that if FilteredUsers table does not exist to be created
        private static List<IUserBase> FilteredUsers { get; } = new List<IUserBase>();
        private readonly IDataAccess _dataAccess;
        private readonly IDataProcessor _dataProcessor;
        public FilterManager(IDataAccess dataAccess, IDataProcessor dataProcessor)
        {
            _dataAccess = dataAccess;
            _dataProcessor = dataProcessor;
        }
        
        public async void InitializeFilter()
        {
            Log.Debug("Initializing filter!");
            var users = await _dataAccess.RetrieveAllUsersFromTable(Enums.DatabaseTables.FilteredUsers);
            if (users == null) return;
            users.RemoveAll(x => x == null);
            var userListTasks = new List<Task<User>>();
            for (var i = 0; i < users.Count; i++)
            {
                userListTasks.Add(_dataProcessor.GetUserAsyncById(users[i].UserID));
            }

            var userList = (await Task.WhenAll(userListTasks)).ToList();
            userList.RemoveAll(x => x == null);
            for (var i = 0; i < userList.Count; i++)
            {
                Log.Debug("{user} {userId} adding to the filter", userList[i].DisplayName, userList[i].Id);
                FilteredUsers.Add(new UserBase(userList[i].DisplayName, userList[i].Id.Trim()));
            }
        }

        public async Task<bool> AddToFiler(IUserBase user)
        {
            if (FilteredUsers.All(x => x.UserId != user.UserId))
            {
                Log.Debug("{user} {userId} adding to the filter", user.DisplayName, user.UserId);
                FilteredUsers.Add(user);
            }
            else FilteredUsers.First(x => x.UserId == user.UserId).DisplayName = user.DisplayName; //NOTE not sure how well this works, but it should.
            return await _dataAccess.ModifyFilteredUsers(Enums.FilteredUsersDatabaseAction.Insert, user.UserId);
        }

        public async Task<bool> RemoveFromFilter(IUserBase user)
        {
            Log.Debug("{user} {userId} removing from the filter", user.DisplayName, user.UserId);
            FilteredUsers.RemoveAll(x => x.UserId == user.UserId);
            return await _dataAccess.ModifyFilteredUsers(Enums.FilteredUsersDatabaseAction.Remove, user.UserId);
        }
        
        public string RetrieveFilteredUsers()
        {
            Log.Debug("Retrieving FilteredUsers");
            if (FilteredUsers.Count <= 0) return "/me Nici un User filtrat!";
            var builder = new StringBuilder();
            builder.Append("/me Useri filtrati:");
            for (var i = 0; i < FilteredUsers.Count; i++)
            {
                builder.Append($" {FilteredUsers[i].DisplayName},");
            }

            return builder.ToString();
        }

        public static bool CheckIfUserFiltered(IUserBase user)
        {
            var stateOfCheck = FilteredUsers.Any(x => x.UserId == user.UserId);
            Log.Debug("FilterCheck requested for {user} {userID} result: {result}", user.DisplayName, user.UserId, stateOfCheck);
            return stateOfCheck;
        }
    }
}