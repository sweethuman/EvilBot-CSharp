using EvilBot.DataStructures.Interfaces;

namespace EvilBot.DataStructures
{
	public class UserStructureData : IUserStructure
	{
		public UserStructureData(string displayName, int id, string userId, string points, string minutes, string rank)
		{
			DisplayName = displayName;
			Id = id;
			UserId = userId;
			Points = points;
			Minutes = minutes;
			Rank = rank;
		}

		public string DisplayName { get; set; }
		public int Id { get; set; }
		public string UserId { get; protected set; }
		public string Points { get; set; }
		public string Minutes { get; set; }
		public string Rank { get; set; }
	}
}