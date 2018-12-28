using System.Collections.Generic;

namespace EvilBot.DataStructures.Interfaces.Comparers
{
	public class ComparerIUserBase : IEqualityComparer<IUserBase>
	{
		public bool Equals(IUserBase x, IUserBase y)
		{
			return x.UserId == y.UserId;
		}

		public int GetHashCode(IUserBase obj)
		{
			return obj.UserId.GetHashCode();
		}
	}
}
