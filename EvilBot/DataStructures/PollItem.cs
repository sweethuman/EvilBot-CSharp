using EvilBot.DataStructures.Interfaces;

namespace EvilBot.DataStructures
{
	public class PollItem : IPollItem
	{
		public PollItem(double itemPoints, string item)
		{
			ItemPoints = itemPoints;
			Item = item;
		}

		public double ItemPoints { get; }
		public string Item { get; }
	}
}