using EvilBot.DataStructures.Interfaces;

namespace EvilBot.DataStructures
{
	public class RankItem : IRankItem
	{
		public RankItem(int id, string name, int requiredPoints)
		{
			Id = id;
			Name = name;
			RequiredPoints = requiredPoints;
		}

		public int Id { get; }
		public string Name { get; }
		public int RequiredPoints { get; }
	}
}