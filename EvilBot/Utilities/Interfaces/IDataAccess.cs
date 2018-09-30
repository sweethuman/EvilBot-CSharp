using System.Threading.Tasks;

namespace EvilBot
{
    public interface IDataAccess
    {
        /// <summary>
        /// Modifies the user DATA using identifier.
        /// </summary>
        /// <param name="userID">The user identifier.</param>
        /// <param name="points">The points.</param>
        /// <param name="minutes">The minutes.</param>
        /// <param name="rank">The rank.</param>
        /// <returns></returns>
        Task ModifierUserIDAsync(string userID, int points = 1, int minutes = 0, int rank = 0);

        /// <summary>
        /// Retrieves the row asynchronous.
        /// </summary>
        /// <param name="userID">The user identifier.</param>
        /// <param name="databaseRow">The database row.</param>
        /// <returns></returns>
        Task<string> RetrieveRowAsync(string userID, Enums.DatabaseRow databaseRow = Enums.DatabaseRow.Points);

        /// <summary>
        /// Modifies the user identifier RANK asynchronous.
        /// </summary>
        /// <param name="userID">The user identifier.</param>
        /// <param name="rank">The rank.</param>
        /// <returns></returns>
        Task ModifyUserIDRankAsync(string userID, int rank);
    }
}