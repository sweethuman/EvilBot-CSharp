using Dapper;
using System;
using System.Collections.Generic;
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

        public async Task AddPointToUsernameAsync(List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> viewers)
        {
            //NOTE make it all just run in background or check if it does or what is the best solution
            for (int i = 0; i < viewers.Count; i++)
            {   //NOTE this was changed, test the method
                if (!(await WriteConnection.QueryAsync<string>($"SELECT Username FROM UserPoints WHERE Username = '{viewers[i].Username}'", new DynamicParameters())).Any())
                {
                    await WriteConnection.ExecuteAsync($"INSERT INTO UserPoints (Username, Points) VALUES ('{viewers[i].Username}', '5')");
                }
                else
                {
                    await WriteConnection.ExecuteAsync($"UPDATE UserPoints SET Points = Points + 5 WHERE Username = '{viewers[i].Username}'");
                }
            }
            Console.WriteLine($"Database updated! Accounts present: {viewers.Count}");
        }

        public async Task<string> RetrievePointsAsync(string username)
        {
            var output = await RetrieveConnection.QueryAsync<string>($"SELECT Points FROM UserPoints WHERE Username = '{username}'", new DynamicParameters());
            if (!output.Any()) //NOTE this was changed as well, test this method too
            {
                return null;
            }
            return output.ToList()[0];
        }

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}