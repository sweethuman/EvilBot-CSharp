using System.Threading.Tasks;

namespace EvilBot
{
    public interface IDataAccess
    {
        Task ModifierUserIDAsync(string userID, int points = 1, int minutes = 0);

        Task<string> RetrieveRowAsync(string userID, Enums.DatabaseRow databaseRow = Enums.DatabaseRow.Points);
    }
}