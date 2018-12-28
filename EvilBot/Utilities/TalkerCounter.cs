using System.Collections.Generic;
using System.Linq;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
	internal class TalkerCounter : ITalkerCounter
	{
		private List<IUserBase> Talkers { get; set; } = new List<IUserBase>();

		public void AddTalker(IUserBase user)
		{
			Log.Debug("AddTalker ran for {User}({userID})", user.DisplayName, user.UserId);
			if (Talkers.All(x => x.UserId != user.UserId)) Talkers.Add(user);
		}

		public bool CheckIfTalker(string userId)
		{
			return Talkers.Exists(x => x.UserId == userId);
		}

		public List<IUserBase> ClearTalkers()
		{
			var output = Talkers;
			Talkers = new List<IUserBase>();
			return output;
		}
	}
}