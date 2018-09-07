using Dapper;
using Serilog;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace EvilBot
{
    public class SqliteDataAccess
    {
        private IDbConnection RetrieveConnection { get; } = new SQLiteConnection(LoadConnectionString("read_only"));
        private IDbConnection WriteConnection { get; } = new SQLiteConnection(LoadConnectionString());

        public async Task<string> RetrievePointsAsync(string userID)
        {
            if (userID == null)
            {
                return null;
            }

            var output = await RetrieveConnection.QueryAsync<string>($"SELECT Points FROM UserPoints WHERE UserID = '{userID}'", new DynamicParameters()).ConfigureAwait(false);
            if (!output.Any()) //NOTE this was changed as well, test this method too
            {
                return null;
            }
            return output.ToList()[0];
        }

        public async Task AddPointToUserID(string userID)
        {
            if (userID == null)
            {
                return;
            }

            if (!(await WriteConnection.QueryAsync<string>($"SELECT UserID from UserPoints WHERE UserID = '{userID}'", new DynamicParameters()).ConfigureAwait(false)).Any())
            {
                Log.Debug("Added a new User to Database with {UserID}", userID);
                await WriteConnection.ExecuteAsync($"INSERT INTO UserPoints (UserID, Points) VALUES ('{userID}', '1')").ConfigureAwait(false);
            }
            else
            {
                Log.Debug("Updated a User with {UserID}", userID);
                await WriteConnection.ExecuteAsync($"UPDATE UserPoints SET Points = Points + 1 WHERE UserID = '{userID}'").ConfigureAwait(false);
            }
        }

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}