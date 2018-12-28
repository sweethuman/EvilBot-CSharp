using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
	public interface IFilterManager
	{
		Task InitializeFilterAsync();

		Task<bool> AddToFilterAsync(IUserBase user);

		Task<bool> RemoveFromFilterAsync(IUserBase user);

		List<IUserBase> RetrieveFilteredUsers();

		/// <param name="userId">Check if userId is in the filter.</param>
		bool CheckIfUserIdFiltered(string userId);
	}
}