using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
    public interface IFilterManager
    {
        void InitializeFilter();
        Task AddToFiler(IUserBase user);
        Task<bool> RemoveFromFilter(IUserBase user);
        string RetrieveFilteredUsers();
    }
}