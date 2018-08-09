using Dapper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace EvilBot
{
    public class SqliteDataAccess
    {
        public static void AddPointToUsername(List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> viewers)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                for (int i = 0; i < viewers.Count; i++)
                {
                    if (!cnn.Query<string>($"SELECT Username FROM UserPoints WHERE Username = '{viewers[i].Username}'", new DynamicParameters()).ToList().Any())
                    {
                        cnn.Execute($"INSERT INTO UserPoints (Username, Points) VALUES ('{viewers[i].Username}', '5')");
                    }
                    else
                    {
                        cnn.Execute($"UPDATE UserPoints SET Points = Points + 5 WHERE Username = '{viewers[i].Username}'");
                    }
                    Console.WriteLine("Accessing Database!");
                }
            }
        }

        public static string RetrievePoints(string username)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString("read_only")))
            {
                var output = cnn.Query<string>($"SELECT Points FROM UserPoints WHERE Username = '{username}'");
                if (!output.ToList().Any())
                {
                    return null;
                }
                return output.ToList()[0];
            }
        }

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}