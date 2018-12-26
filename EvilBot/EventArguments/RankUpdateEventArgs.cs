using System;

namespace EvilBot.EventArguments
{
	public class RankUpdateEventArgs : EventArgs
	{
		public string Name { get; set; }
		public string Rank { get; set; }
	}
}