using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Managers.Interfaces
{
	public interface IFilterManager
	{
		Task<bool> AddToFilterAsync(IUserBase user);

		Task<bool> RemoveFromFilterAsync(IUserBase user);

		List<IUserBase> RetrieveFilteredUsers();

		/// <param name="userId">Check if userId is in the filter.</param>
		bool CheckIfUserIdFiltered(string userId);

		bool CheckIfUserFiltered(IUserBase user);
	}
}
