using System.Collections.Generic;
using System.Linq;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
	public class SetsPresenceCounter : IPresenceCounter
	{
		private HashSet<string> PresentUserIds { get; set; } = new HashSet<string>();

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
			var tempIds = PresentUserIds;
			PresentUserIds = new HashSet<string>();
			return tempIds.ToList();
		}
	}
}
