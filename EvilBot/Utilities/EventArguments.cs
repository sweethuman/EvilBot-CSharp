using System;

namespace EvilBot.Utilities
{
	public class RankUpdateEventArgs : EventArgs
	{
		public string Name { get; set; }
		public string Rank { get; set; }
	}
}