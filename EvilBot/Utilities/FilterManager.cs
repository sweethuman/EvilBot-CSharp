using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Utilities.Interfaces;
using EvilBot.Utilities.Resources;
using EvilBot.Utilities.Resources.Interfaces;
using Serilog;

namespace EvilBot.Utilities
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

		//NOTE this is async but returns void
		public async void InitializeFilter()
		{
			Log.Debug("Initializing filter!");
			var users = await _dataAccess.RetrieveAllUsersFromTable(Enums.DatabaseTables.FilteredUsers);
			if (users == null) return;
			users.RemoveAll(x => x == null);
			var userListTasks = users.Select(t => _apiRetriever.GetUserAsyncById(t.UserId)).ToList();

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
			else
			{
				FilteredUsers.First(x => x.UserId == user.UserId).DisplayName =
					user.DisplayName; //NOTE not sure how well this works, but it should.
			}

			return await _dataAccess.ModifyFilteredUsers(Enums.FilteredUsersDatabaseAction.Insert, user.UserId);
		}

		public async Task<bool> RemoveFromFilter(IUserBase user)
		{
			Log.Debug("{user} {userId} removing from the filter", user.DisplayName, user.UserId);
			FilteredUsers.RemoveAll(x => x.UserId == user.UserId);
			return await _dataAccess.ModifyFilteredUsers(Enums.FilteredUsersDatabaseAction.Remove, user.UserId);
		}

		public List<IUserBase> RetrieveFilteredUsers()
		{
			Log.Debug("Retrieving FilteredUsers");
			return FilteredUsers;
		}
		//NOTE maybe i should only be working with userId's
		public bool CheckIfUserFiltered(IUserBase user)
		{
			var stateOfCheck = FilteredUsers.Any(x => x.UserId == user.UserId);
			Log.Debug("FilterCheck requested for {user} {userID} result: {result}", user.DisplayName, user.UserId,
				stateOfCheck);
			return stateOfCheck;
		}
	}
}