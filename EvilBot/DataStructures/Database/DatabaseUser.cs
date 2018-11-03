using EvilBot.DataStructures.Database.Interfaces;

namespace EvilBot.DataStructures.Database
{
	public class DatabaseUser : IDatabaseUser
	{
		public int Id { get; set; }
		public string UserId { get; set; }
		public string Points { get; set; }
		public string Minutes { get; set; }
		public string Rank { get; set; }
	}
}