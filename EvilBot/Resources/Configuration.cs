using System.Configuration;
using EvilBot.Resources.Interfaces;

namespace EvilBot.Resources
{
	public class Configuration : IConfiguration
	{
		public float PointsMultiplier { get; } = float.Parse(ConfigurationManager.AppSettings.Get("pointsMultiplier"));

		public float MessageRepeaterMinutes { get; } = float.Parse(ConfigurationManager.AppSettings.Get("messageRepeaterMinutes"));

		public int BitsPointsMultiplier { get; } = int.Parse(ConfigurationManager.AppSettings.Get("bitsToPointsMultipliers"));

		public float LurkerMinutes { get; } = 10;

		public int LurkerPoints { get; } = 1;

		public float TalkerMinutes { get; } = 1;

		public int TalkerPoints { get; } = 1;
	}
}
