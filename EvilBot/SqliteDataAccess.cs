using Dapper;
using Serilog;
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
        private List<string> temporaryTalkers;

        public async Task AddLurkerPointToUsernameAsync(List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> viewers)
        {
            for (int i = 0; i < viewers.Count; i++)
            {
                if (!(await WriteConnection.QueryAsync<string>($"SELECT Username FROM UserPoints WHERE Username = '{viewers[i].Username}'", new DynamicParameters())).Any())
                {
                    Log.Debug("New Lurker: {Username}", viewers[i].Username);
                    await WriteConnection.ExecuteAsync($"INSERT INTO UserPoints (Username, Points) VALUES ('{viewers[i].Username}', '1')");
                }
                else
                {
                    Log.Debug("Updating Lurker: {Username}", viewers[i].Username);
                    await WriteConnection.ExecuteAsync($"UPDATE UserPoints SET Points = Points + 1 WHERE Username = '{viewers[i].Username}'");
                }
            }
            Console.WriteLine($"Database updated! Lurkers present: {viewers.Count}");
        }

        public async Task AddPointToUsernameAsync()
        {
            temporaryTalkers = PointCounter.Talkers;
            PointCounter.Talkers = new List<string>();
            for (int i = 0; i < temporaryTalkers.Count; i++)
            {
                if (!(await WriteConnection.QueryAsync<string>($"SELECT Username FROM UserPoints WHERE Username = '{temporaryTalkers[i]}'", new DynamicParameters())).Any())
                {
                    Log.Debug("New Talker: {Username}", temporaryTalkers[i]);
                    await WriteConnection.ExecuteAsync($"INSERT INTO UserPoints (Username, Points) VALUES ('{temporaryTalkers[i]}', '1')");
                }
                else
                {
                    Log.Debug("Updating Talker: {Username}", temporaryTalkers[i]);
                    await WriteConnection.ExecuteAsync($"UPDATE UserPoints SET Points = Points + 1 WHERE Username = '{temporaryTalkers[i]}'");
                }
            }
            Console.WriteLine($"Database updated! Talkers present: {temporaryTalkers.Count}");
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