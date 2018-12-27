namespace EvilBot.DataStructures.Interfaces
{
	public interface IPollItem
	{
		int Id { get; }
		double Points { get; set; }
		string Name { get; }
	}
}