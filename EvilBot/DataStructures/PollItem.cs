using EvilBot.DataStructures.Interfaces;

namespace EvilBot.DataStructures
{
	public class PollItem : IPollItem
	{
		public PollItem(int itemId, double itemPoints, string item)
		{
			ItemId = itemId;
			ItemPoints = itemPoints;
			Item = item;
		}
		
		public int ItemId { get; }
		public double ItemPoints { get; }
		public string Item { get; }
	}
}