using System.Collections.Generic;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
	public class PresenceCounter : IPresenceCounter
	{
		private List<string> PresentUserIds { get; set; } = new List<string>();

		public void MakePresent(string userId)
		{
			PresentUserIds.Add(userId);
			Log.Debug("Making present {UserId}...", userId);
		}

		public bool CheckIfPresent(string userId)
		{
			Log.Debug("Checking presence for {userid}", userId);
			return PresentUserIds.Contains(userId);
		}

		public List<string> ClearPresenceCounter()
		{
			var tempPresent = PresentUserIds;
			PresentUserIds = new List<string>();
			return tempPresent;
		}
	}
}