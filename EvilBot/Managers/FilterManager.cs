using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using Serilog;

namespace EvilBot.Managers
{
	public class FilterManager : IFilterManager
	{
		private readonly IApiRetriever _apiRetriever;
		private readonly IDataAccess _dataAccess;

		public FilterManager(IDataAccess dataAccess, IApiRetriever apiRetriever)
		{
			_dataAccess = dataAccess;
			_apiRetriever = apiRetriever;
		}

		//TODO add somewhere code that if FilteredUsers table does not exist to be created
		private List<IUserBase> FilteredUsers { get; } = new List<IUserBase>();

		public async Task InitializeFilterAsync()
		{
			Log.Debug("Initializing filter!");
			var users = await _dataAccess.RetrieveAllUsersFromTableAsync(Enums.DatabaseTables.FilteredUsers)
				.ConfigureAwait(false);
			if (users == null) return;
			users.RemoveAll(x => x == null);

			//NOTE if GetUsersHelixAsync fails the program shouldn't start
			var userIds = users.Select(x => x.UserId).ToList();
			var userList = await _apiRetriever.GetUsersHelixAsync(userIds).ConfigureAwait(false);
			userList.RemoveAll(user => user == null);

			foreach (var user in userList)
			{
				Log.Debug("{user} {userId} adding to the filter", user.DisplayName, user.Id);
				FilteredUsers.Add(new UserBase(user.DisplayName, user.Id.Trim()));
			}
		}

		public Task<bool> AddToFilterAsync(IUserBase user)
		{
			//NO EXCEPTION SHOULD OR CAN BE THROWN HERE
			//await eliding is okay because the methods above this will not throw exceptions, or shouldn't
			if (FilteredUsers.All(x => x.UserId != user.UserId))
			{
				Log.Debug("{user} {userId} adding to the filter", user.DisplayName, user.UserId);
				FilteredUsers.Add(user);
			}
			else
			{
				FilteredUsers.First(x => x.UserId == user.UserId).DisplayName =
					user.DisplayName;
			}

			return _dataAccess.ModifyFilteredUsersAsync(Enums.FilteredUsersDatabaseAction.Insert, user.UserId);
		}

		public Task<bool> RemoveFromFilterAsync(IUserBase user)
		{
			//NO EXCEPTION SHOULD OR CAN BE THROWN HERE
			//await eliding is okay because the methods above this will not throw exceptions, or shouldn't
			Log.Debug("{user} {userId} removing from the filter", user.DisplayName, user.UserId);
			FilteredUsers.RemoveAll(x => x.UserId == user.UserId);
			return _dataAccess.ModifyFilteredUsersAsync(Enums.FilteredUsersDatabaseAction.Remove, user.UserId);
		}

		public List<IUserBase> RetrieveFilteredUsers()
		{
			Log.Debug("Retrieving FilteredUsers");
			return FilteredUsers;
		}

		public bool CheckIfUserFiltered(IUserBase user)
		{
			var stateOfCheck = FilteredUsers.Any(x => x.UserId == user.UserId);
			Log.Debug("FilterCheck requested for {user} {userID} result: {result}", user.DisplayName, user.UserId,
				stateOfCheck);
			return stateOfCheck;
		}

		public bool CheckIfUserIdFiltered(string userId)
		{
			var stateOfCheck = FilteredUsers.Any(x => x.UserId == userId);
			Log.Debug("FilterCheck requested for {userID} result: {result}", userId,
				stateOfCheck);
			return stateOfCheck;
		}
	}
}
