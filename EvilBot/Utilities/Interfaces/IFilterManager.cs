using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
	public interface IFilterManager
	{
		Task InitializeFilter();
		
		Task<bool> AddToFiler(IUserBase user);
		
		Task<bool> RemoveFromFilter(IUserBase user);
		
		List<IUserBase> RetrieveFilteredUsers();
		
		/// <param name="user">Only users Id to identify filter. The DisplayName is used for Logging and can be ignored.</param>
		bool CheckIfUserFiltered(IUserBase user);
	}
}
