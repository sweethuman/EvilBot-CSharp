using System.Threading.Tasks;

namespace EvilBot
{
    public interface IDataAccess
    {
        Task AddPointToUserID(string userID, int points = 1, int minutes = 0);

        Task<string> RetrieveRowAsync(string userID, bool retrieveMinutes = false);
    }
}