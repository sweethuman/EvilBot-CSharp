using System.Collections.Generic;
using Serilog;

namespace EvilBot.Utilities
{
    public static class PresenceCounter
    {
        public static readonly List<string> PresentUserIds = new List<string>();

        public static bool IsNotPresent(string userId)
        {
            Log.Debug("Checking presence for {userid}", userId);
            if (PresentUserIds.Contains(userId))
                return false;
            PresentUserIds.Add(userId);
            Log.Debug("{UserId} not present. Adding...", userId);
            return true;
        }
    }
}