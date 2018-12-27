using System.Configuration;
using EvilBot.Resources.Interfaces;

namespace EvilBot.Resources
{
	public class Configuration : IConfiguration
	{
		public float PointsMultiplier => float.Parse(ConfigurationManager.AppSettings.Get("pointsMultiplier"));

		public float MessageRepeaterMinutes =>
			float.Parse(ConfigurationManager.AppSettings.Get("messageRepeaterMinutes"));

		public int BitsPointsMultiplier => int.Parse(ConfigurationManager.AppSettings.Get("bitsToPointsMultipliers"));
	}
}