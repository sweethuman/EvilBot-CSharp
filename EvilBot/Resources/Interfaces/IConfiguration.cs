namespace EvilBot.Resources.Interfaces
{
	public interface IConfiguration
	{
		float PointsMultiplier { get; }
		float MessageRepeaterMinutes { get; }
		int BitsPointsMultiplier { get; }
		float LurkerMinutes { get; }
		int LurkerPoints { get; }
		float TalkerMinutes { get; }
		int TalkerPoints { get; }
		string BotToken { get; }
		string BotUsername { get; }
		string ChannelName { get; }
		string ClientId { get; }
	}
}
