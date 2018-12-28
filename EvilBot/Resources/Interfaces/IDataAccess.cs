using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Database.Interfaces;

namespace EvilBot.Resources.Interfaces
{
	public interface IDataAccess
	{
		/// <summary>
		///     Modifies the user DATA using identifier.
		/// </summary>
		/// <param name="userId">The user identifier.</param>
		/// <param name="points">The points.</param>
		/// <param name="minutes">The minutes.</param>
		/// <param name="rank">The rank.</param>
		/// <returns></returns>
		Task ModifierUserIdAsync(string userId, int points = 1, int minutes = 0, int rank = 0);

		/// <summary>
		///     Modifies the user identifier RANK asynchronous.
		/// </summary>
		/// <param name="userId">The user identifier.</param>
		/// <param name="rank">The rank.</param>
		/// <returns></returns>
		Task ModifyUserIdRankAsync(string userId, int rank);

		/// <summary>
		///     Retrieves a user from a table.
		/// </summary>
		/// <param name="table">The tables you can retrieve users from.</param>
		/// <param name="userId">If no userId is given it will retrieve all users.</param>
		/// <remarks>In case of FilteredUsers only Id and UserId is populated.</remarks>
		/// <returns>Returns a container with all the resources it could get. Not guaranteed it will populate all.</returns>
		Task<IDatabaseUser> RetrieveUserFromTableAsync(Enums.DatabaseTables table, string userId = null);

		/// <summary>
		///     Modifies the list of FilteredUsers in the database.
		/// </summary>
		/// <param name="userId">The user identifier.</param>
		/// <param name="databaseAction">Represents the action to do in the database</param>
		/// <returns>True if the user was removed or added. False means database was not changed.</returns>
		Task<bool> ModifyFilteredUsersAsync(Enums.FilteredUsersDatabaseAction databaseAction, string userId);

		/// <summary>
		///     Closes the database connections.
		/// </summary>
		void Close();

		/// <summary>
		///     Retrieves all the users in a certain table.
		/// </summary>
		/// <param name="table">The table to retrieve users from.</param>
		/// <remarks>In case of FilteredUsers only Id and UserId is populated.</remarks>
		/// <returns>Returns a container with all the resources it could get. Not guaranteed it will populate all.</returns>
		Task<List<IDatabaseUser>> RetrieveAllUsersFromTableAsync(Enums.DatabaseTables table);


		/// <summary>
		///     Retrieve a limited amout of users from the database.
		/// </summary>
		/// <param name="table">Specify the table where to get from.</param>
		/// <param name="limit">Number of users to get.</param>
		/// <param name="orderRow">Only use this if you are pulling users from UserPoints.</param>
		/// <returns>A List of users filled with data. In case of Filtered Users only Id and UserId.</returns>
		Task<List<IDatabaseUser>> RetrieveNumberOfUsersFromTableAsync
		(Enums.DatabaseTables table, int limit,
			Enums.DatabaseUserPointsOrderRow orderRow = Enums.DatabaseUserPointsOrderRow.None);
	}
}