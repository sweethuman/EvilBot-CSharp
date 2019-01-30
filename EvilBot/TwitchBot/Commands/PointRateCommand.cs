using System.Threading.Tasks;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using EvilBot.TwitchBot.Commands.Interfaces;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class PointRateCommand : ITwitchCommand
	{
		private string PointRateString { get; }

		public PointRateCommand(IConfiguration configuration)
		{
			PointRateString = string.Format(StandardMessages.PointRateString, configuration.LurkerPoints,
				configuration.LurkerMinutes, configuration.TalkerPoints, configuration.TalkerMinutes);
		}

		public bool NeedMod { get; } = false;
		public Task<string>  ProcessorAsync(OnChatCommandReceivedArgs e) => Task.FromResult(PointRateString);
	}
}
