using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
    public class SqliteDataAccess : IDataAccess
    {
        private IDbConnection RetrieveConnection { get; }
        private IDbConnection WriteConnection { get; }

        public SqliteDataAccess()
        {
            RetrieveConnection = new SQLiteConnection(LoadConnectionString("read_only"));
            WriteConnection = new SQLiteConnection(LoadConnectionString());
            
        }

        //t: MAKE a function that retrieves all three attributes at once for performance reasons
        public async Task<string> RetrieveRowAsync(string userId, Enums.DatabaseRow databaseRow = Enums.DatabaseRow.Points)
        {
            if (RetrieveConnection.State != ConnectionState.Open)
            {
                RetrieveConnection.Open();
            }
            if (userId == null)
            {
                return null;
            }
            var column = databaseRow.ToString();

            var output = (await RetrieveConnection.QueryAsync<string>($"SELECT {column} FROM UserPoints WHERE UserID = '{userId}'", new DynamicParameters()).ConfigureAwait(false)).ToList();
            if (output.Any()) return output.ToList()[0];
            Log.Warning("Asked for inexistent userID in database: {userID}", userId);
            return null;
        }

        public async Task ModifierUserIdAsync(string userId, int points = 1, int minutes = 0, int rank = 0)
        {
            if (WriteConnection.State != ConnectionState.Open)
            {
                WriteConnection.Open();
            }
            if (userId == null)
            {
                return;
            }

            //on mysql you can do ON DUPLICATE KEY
            if (!(await WriteConnection.QueryAsync<string>($"SELECT UserID from UserPoints WHERE UserID = '{userId}'", new DynamicParameters()).ConfigureAwait(false)).Any())
            {
                await WriteConnection.ExecuteAsync($"INSERT INTO UserPoints (UserID, Points, Minutes, Rank) VALUES ('{userId}', '{points}', '{minutes}', '{rank}')").ConfigureAwait(false);
                Log.Information("Added a new User to Database with {UserID}", userId);
            }
            else
            {
                await WriteConnection.ExecuteAsync($"UPDATE UserPoints SET Points = Points + {points}, Minutes = Minutes + {minutes} WHERE UserID = '{userId}'").ConfigureAwait(false);
                Log.Information("{userID} modified {minutes}m and {points}", userId, minutes, points);
            }
        }

        public async Task ModifyUserIdRankAsync(string userId, int rank)
        {
            if (WriteConnection.State != ConnectionState.Open)
            {
                WriteConnection.Open();
            }
            if (userId == null)
            {
                return;
            }

            Log.Information("Advanced a User with {UserID} with [{Rank}]", userId, rank);
            try
            {
                await WriteConnection.ExecuteAsync($"UPDATE UserPoints SET Rank = {rank} WHERE UserID = '{userId}'").ConfigureAwait(false);
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "ModifyUserIDRankAsync blew up with: {UserID} {Rank}", userId, rank);
            }
        }

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}