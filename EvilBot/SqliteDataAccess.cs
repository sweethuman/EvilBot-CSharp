using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using Dapper;

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

        public static void SaveUsername()
        {
            string whataname = "gaogl";
            string whatanamer = "ffs";
            using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
            {
                if(!cnn.Query<string>($"SELECT Username FROM UserPoints WHERE Username = '{whatanamer}'", new DynamicParameters()).ToList().Any())
                {
                    cnn.Execute($"INSERT INTO UserPoints (Username, Points) VALUES ('{whatanamer}', '5')");
                }
                cnn.Execute($"UPDATE UserPoints SET Points = Points + 1 WHERE Username = '{whataname}'");
            }
        }

        private static string LoadConnectionString(string id = "Default")
        {
            return ConfigurationManager.ConnectionStrings[id].ConnectionString;
        }
    }
}
