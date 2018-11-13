using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
	public interface IFilterManager
	{
		void InitializeFilter();
		Task<bool> AddToFiler(IUserBase user);
		Task<bool> RemoveFromFilter(IUserBase user);
		string RetrieveFilteredUsers();
		
		/// <param name="user">Only users Id to identify filter. The DisplayName is used for Logging and can be ignored.</param>
		bool CheckIfUserFiltered(IUserBase user);
	}
}