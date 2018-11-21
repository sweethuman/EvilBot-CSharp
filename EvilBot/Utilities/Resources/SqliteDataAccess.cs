using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using EvilBot.DataStructures.Database;
using EvilBot.DataStructures.Database.Interfaces;
using EvilBot.Utilities.Resources.Interfaces;
using Serilog;

namespace EvilBot.Utilities.Resources
{
	public class SqliteDataAccess : IDataAccess
	{
		public SqliteDataAccess()
		{
			RetrieveConnection = new SQLiteConnection(LoadConnectionString("read_only"));
			WriteConnection = new SQLiteConnection(LoadConnectionString());
		}

		private IDbConnection RetrieveConnection { get; }
		private IDbConnection WriteConnection { get; }

		public async Task ModifierUserIdAsync(string userId, int points = 1, int minutes = 0, int rank = 0)
		{
			if (WriteConnection.State != ConnectionState.Open) WriteConnection.Open();
			if (userId == null) return;
			await WriteConnection
				.ExecuteAsync(
					"INSERT INTO UserPoints (UserID, Points, Minutes, Rank) VALUES (@UserId, @Points, @Minutes, @Rank) ON CONFLICT(UserID) DO UPDATE SET Points = Points + @Points, Minutes = Minutes + @Minutes",
					new {UserId = userId, Points = points, Minutes = minutes, Rank = rank})
				.ConfigureAwait(false);
			Log.Information("{userID} modified/added {minutes}m and {points}", userId, minutes, points);
		}

		public async Task ModifyUserIdRankAsync(string userId, int rank)
		{
			if (WriteConnection.State != ConnectionState.Open) WriteConnection.Open();
			if (userId == null) return;

			Log.Information("Advancing a User with {UserID} with [{Rank}]", userId, rank);
			try
			{
				await WriteConnection.ExecuteAsync("UPDATE UserPoints SET Rank = @Rank WHERE UserID = @UserId", new {Rank = rank, UserID = userId})
					.ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "ModifyUserIDRankAsync blew up with: {UserID} {Rank}", userId, rank);
			}
		}
		
		public async Task<bool> ModifyFilteredUsers(Enums.FilteredUsersDatabaseAction databaseAction, string userId)
		{
			if (WriteConnection.State != ConnectionState.Open) WriteConnection.Open();
			Log.Debug("Modifying filtered {userId}", userId);
			switch (databaseAction)
			{
				case Enums.FilteredUsersDatabaseAction.Remove:
				{
					var rowsAffected =
						await WriteConnection.ExecuteAsync("DELETE FROM FilteredUsers WHERE UserID = @UserId ", new {UserId = userId});
					return rowsAffected > 0;
				}
				case Enums.FilteredUsersDatabaseAction.Insert:
				{
					var rowsAffected =
						await WriteConnection.ExecuteAsync(
							"INSERT INTO FilteredUsers (UserID) VALUES (@UserId) ON CONFLICT DO NOTHING", new {UserId = userId});
					return rowsAffected > 0;
				}
				default:
					Log.Warning("NOTHING HAPPENED IN MODIFY FILTERED USERS NOTHING!!!! {databaseAction} {userId}",
						databaseAction.ToString(), userId);
					return false;
			}
		}

		public async Task<List<IDatabaseUser>> RetrieveAllUsersFromTable(Enums.DatabaseTables table)
		{
			if (RetrieveConnection.State != ConnectionState.Open) RetrieveConnection.Open();
			var retrievingTable = table.ToString();
			Log.Debug("Retrieving all users from table {table}", retrievingTable);
			var output =
				(await RetrieveConnection.QueryAsync<DatabaseUser>($"SELECT * FROM {retrievingTable}",
					new DynamicParameters())).ToList();
			var results = output.ToList<IDatabaseUser>();
			if (output.Any()) return results;
			Log.Warning("{table} table is empty!", retrievingTable);
			return null;
		}

		public async Task<IDatabaseUser> RetrieveUserFromTable(Enums.DatabaseTables table, string userId)
		{
			if (userId == null) return null;
			if (RetrieveConnection.State != ConnectionState.Open) RetrieveConnection.Open();
			var retrievingTable = table.ToString();
			Log.Debug("Retrieving {userId} from table {table}", userId, retrievingTable);
			//TODO: add QueryFirstOrDefault after you know what default returns
			var output = (await RetrieveConnection.QueryAsync<DatabaseUser>(
				$"SELECT * FROM {retrievingTable} WHERE UserID = @UserId ", new {RetrievingTable = retrievingTable ,UserId = userId})).ToList();
			if (output.Any()) return output.First();
			Log.Warning("{userId} not in table {table}", userId, retrievingTable);
			return null;
		}

		public async Task<List<IDatabaseUser>> RetrieveNumberOfUsersFromTable
			(Enums.DatabaseTables table, int limit, Enums.DatabaseUserPointsOrderRow orderRow = Enums.DatabaseUserPointsOrderRow.None)
		{
			if (RetrieveConnection.State != ConnectionState.Open) RetrieveConnection.Open();
			var retrievingTable = table.ToString();
			Log.Debug("Retrieving all users from table {table}", retrievingTable);
			var orderString = "";
			if (orderRow != Enums.DatabaseUserPointsOrderRow.None)
				orderString = $"ORDER BY {orderRow.ToString()}";
			if(table != Enums.DatabaseTables.UserPoints && orderRow != Enums.DatabaseUserPointsOrderRow.None)
				throw new Exception("Bad parameter. When using other than UserPoints table orderRow needs to be None.");
			var output =
				(await RetrieveConnection.QueryAsync<DatabaseUser>($"SELECT * FROM {retrievingTable} {orderString} DESC LIMIT {limit}",
					new DynamicParameters())).ToList();
			var results = output.ToList<IDatabaseUser>();
			if (output.Any()) return results;
			Log.Warning("{table} table is empty!", retrievingTable);	
			return null;
		}
		public void Close()
		{
			Log.Debug("Closing database connections!");
			WriteConnection.Close();
			RetrieveConnection.Close();
			WriteConnection.Dispose();
			RetrieveConnection.Dispose();
			Log.Debug("Closed database success!");
		}

		private static string LoadConnectionString(string id = "Default")
		{
			return ConfigurationManager.ConnectionStrings[id].ConnectionString;
		}
	}
}
