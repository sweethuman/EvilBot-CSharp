using System.Collections.Generic;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Utilities.Interfaces
{
	public interface ITalkerCounter
	{
		void AddTalker(IUserBase user);
		bool CheckIfTalker(string userId);
		List<IUserBase> ClearTalkers();
	}
}