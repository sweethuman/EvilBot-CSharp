using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using Dapper;
using TwitchLib;
using TwitchLib.Api.Models.v5.Users;
using TwitchLib.Api.Models.v5.Streams;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Api;
using TwitchLib.Api.Enums;

namespace EvilBot
{
    public class SqliteDataAccess
    {
        public static List<string> LoadUsernames()
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                var output = cnn.Query<string>("SELECT Username FROM UserPoints", new DynamicParameters());
                return output.ToList();
            }
        }

        public static void AddPointToUsername(List<TwitchLib.Api.Models.Undocumented.Chatters.ChatterFormatted> viewers)
        {
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                for(int i=0; i<viewers.Count; i++)
                {

                    if(!cnn.Query<string>($"SELECT Username FROM UserPoints WHERE Username = '{viewers[i].Username}'", new DynamicParameters()).ToList().Any())
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

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
