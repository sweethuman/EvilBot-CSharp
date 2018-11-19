using System.Collections.Generic;
using Serilog;

namespace EvilBot.Utilities
{
    public static class PresenceCounter
    {
        public static List<string> presentUserIds = new List<string>();

        public static bool IsNotPresent(string userId)
        {
            Log.Debug("Checking presence for {userid}", userId);
            if (presentUserIds.Contains(userId))
                return false;
            presentUserIds.Add(userId);
            Log.Debug("{UserId} not present. Adding...", userId);
            return true;
        }
    }
}