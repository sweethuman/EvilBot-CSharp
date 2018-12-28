using System.Collections.Generic;
using System.Linq;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.DataStructures.Interfaces.Comparers;
using EvilBot.Utilities.Interfaces;
using Serilog;

namespace EvilBot.Utilities
{
	public class SetsTalkerCounter : ITalkerCounter
	{
		private HashSet<IUserBase> Talkers { get; set; } = new HashSet<IUserBase>(new ComparerIUserBase());

		public void AddTalker(IUserBase user)
		{
			Log.Debug("AddTalker ran for {User}({userID})", user.DisplayName, user.UserId);
			Talkers.Add(user);
		}

		public bool CheckIfTalker(string userId)
		{
			return Talkers.Contains(new UserBase("NO NAME", userId));
		}

		public List<IUserBase> ClearTalkers()
		{
			var tempTalkers = Talkers;
			Talkers = new HashSet<IUserBase>(new ComparerIUserBase());
			return tempTalkers.ToList();
		}
	}
}
