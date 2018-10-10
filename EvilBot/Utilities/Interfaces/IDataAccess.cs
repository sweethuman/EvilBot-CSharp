using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Database.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
    public interface IDataAccess
    {
        /// <summary>
        /// Modifies the user DATA using identifier.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="points">The points.</param>
        /// <param name="minutes">The minutes.</param>
        /// <param name="rank">The rank.</param>
        /// <returns></returns>
        Task ModifierUserIdAsync(string userId, int points = 1, int minutes = 0, int rank = 0);

        /// <summary>
        /// Retrieves the row asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="databaseRow">The database row.</param>
        /// <returns></returns>
        Task<string> RetrieveRowAsync(string userId, Enums.DatabaseRow databaseRow = Enums.DatabaseRow.Points);

        /// <summary>
        /// Modifies the user identifier RANK asynchronous.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="rank">The rank.</param>
        /// <returns></returns>
        Task ModifyUserIdRankAsync(string userId, int rank);

        Task<List<IDatabaseUser>> RetrieveAllUsersFromTable();
    }
}