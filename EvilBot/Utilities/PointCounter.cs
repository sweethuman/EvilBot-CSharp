using EvilBot.DataStructures;
using Serilog;
using System.Collections.Generic;
using System.Linq;

namespace EvilBot
{
    internal static class PointCounter
    {
        public static List<UserBase> Talkers { get; private set; } = new List<UserBase>();

        public static void AddMessagePoint(UserBase user)
        {
            Log.Debug("AddMessagePoint ran for {User}({userID})", user.DisplayName, user.UserId);
            if (!Talkers.Any(x => x.UserId == user.UserId))
            {
                Talkers.Add(user);
            }
        }

        public static List<UserBase> ClearTalkerPoints()
        {
            List<UserBase> output = Talkers;
            Talkers = new List<UserBase>();
            return output;
        }
    }
}