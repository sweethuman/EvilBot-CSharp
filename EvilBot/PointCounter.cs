using Serilog;
using System.Collections.Generic;

namespace EvilBot
{
    internal static class PointCounter
    {
        public static List<string> Talkers { get; private set; } = new List<string>();

        public static void AddMessagePoint(string username)
        {
            Log.Debug("AddMessagePoint ran");
            if (!Talkers.Contains(username))
            {
                Talkers.Add(username);
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