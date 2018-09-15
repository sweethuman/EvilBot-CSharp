using Serilog;
using System.Collections.Generic;

namespace EvilBot
{
    internal static class PointCounter
    {
        public static List<string> Talkers { get; private set; } = new List<string>();

        public static void AddMessagePoint(string userID)
        {
            Log.Debug("AddMessagePoint ran for {userID}", userID);
            if (!Talkers.Contains(userID))
            {
                Talkers.Add(userID);
            }
        }

        public static List<string> ClearTalkerPoints()
        {
            List<string> output = Talkers;
            Talkers = new List<string>();
            return output;
        }
    }
}