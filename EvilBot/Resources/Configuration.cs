using System.Configuration;
using EvilBot.Resources.Interfaces;

namespace EvilBot.Resources
{
	public class Configuration : IConfiguration
	{

		public Configuration()
		{
				var configFileMap = new ExeConfigurationFileMap();
				configFileMap.ExeConfigFilename = "credentials.config";

				var config =
					ConfigurationManager.OpenMappedExeConfiguration(configFileMap, ConfigurationUserLevel.None);

				BotToken = config.AppSettings.Settings["BotToken"].Value;
				BotUsername = config.AppSettings.Settings["BotUsername"].Value;
				ChannelName = config.AppSettings.Settings["ChannelName"].Value;
				ClientId = config.AppSettings.Settings["ClientID"].Value;
		}

		public float PointsMultiplier { get; } = float.Parse(ConfigurationManager.AppSettings.Get("pointsMultiplier"));

		public float MessageRepeaterMinutes { get; } = float.Parse(ConfigurationManager.AppSettings.Get("messageRepeaterMinutes"));

		public int BitsPointsMultiplier { get; } = int.Parse(ConfigurationManager.AppSettings.Get("bitsToPointsMultipliers"));

		public float LurkerMinutes { get; } = 10;

		public int LurkerPoints { get; } = 1;

		public float TalkerMinutes { get; } = 1;

		public int TalkerPoints { get; } = 1;

		public string BotToken { get; }

		public string BotUsername { get; }

		public string ChannelName { get; }

		public string ClientId { get; }
	}
}
