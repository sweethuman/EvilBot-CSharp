namespace EvilBot.Resources.Interfaces
{
	public interface IConfiguration
	{
		float PointsMultiplier { get; }
		float MessageRepeaterMinutes { get; }
		int BitsPointsMultiplier { get; }
	}
}