using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
    public interface IFilterManager
    {
        void InitializeFilter();
        void AddToFiler(IUserBase user);
        void RemoveFromFilter(IUserBase user);
        string RetrieveFilteredUsers();
        bool CheckIfUserFiltered(IUserBase user);
    }
}