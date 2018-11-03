namespace EvilBot.DataStructures.Database.Interfaces
{
	public interface IDatabaseUser
	{
		int Id { get; set; }
		string UserId { get; set; }
		string Points { get; set; }
		string Minutes { get; set; }
		string Rank { get; set; }
	}
}