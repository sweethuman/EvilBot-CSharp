using System.Collections.Generic;

namespace EvilBot.Trackers.Interfaces
{
	public interface IPresenceCounter
	{
		void MakePresent(string userId);
		bool CheckIfPresent(string userId);
		List<string> ClearPresenceCounter();
	}
}