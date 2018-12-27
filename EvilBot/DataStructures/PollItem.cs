using EvilBot.DataStructures.Interfaces;

namespace EvilBot.DataStructures
{
	public class PollItem : IPollItem
	{
		public PollItem(int id, double itemPoints, string item)
		{
			Id = id;
			Points = itemPoints;
			Name = item;
		}
		
		public int Id { get; }
		public double Points { get; set; }
		public string Name { get; }
	}
}