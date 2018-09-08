using System.Threading.Tasks;

namespace EvilBot
{
    public interface IDataAccess
    {
        Task AddPointToUserID(string userID, int points = 1);
        Task<string> RetrievePointsAsync(string userID);
    }
}