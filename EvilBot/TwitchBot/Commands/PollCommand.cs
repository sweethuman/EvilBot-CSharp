using System.Text;
using System.Threading.Tasks;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Enums;
using EvilBot.TwitchBot.Commands.Interfaces;
using EvilBot.Utilities;
using Serilog;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;

namespace EvilBot.TwitchBot.Commands
{
	public class PollCommand : ITwitchCommand
	{
		private readonly IPollManager _pollManager;

		public PollCommand(IPollManager pollManager)
		{
			_pollManager = pollManager;
		}

		private string PollOptionsString { get; set; }

		public bool NeedMod { get; } = false;

		public async Task<string> ProcessorAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList == null || e.Command.ArgumentsAsList.Count == 0)
				return CommandHelpers.ChangeOutputIfMod(e.Command.ChatMessage.UserType,
					StandardMessages.PollMessages.PollDefaultFormat, StandardMessages.PollMessages.PollModFormat);
			switch (e.Command.ArgumentsAsList[0].ToLower())
			{
				case "create":
					if (e.Command.ChatMessage.UserType < UserType.Moderator) return null;
					var pollOptions = e.Command.ArgumentsAsString.Trim().Remove(0, "create".Length).Trim();
					return PollCreateCommand(pollOptions);
				case "end":
					if (e.Command.ChatMessage.UserType < UserType.Moderator) return null;
					return PollEndCommand();
				case "vote":
					return await PollVoteCommandAsync(e).ConfigureAwait(false);
				case "stats":
					return PollStatsCommand();
				default:
					return CommandHelpers.ChangeOutputIfMod(e.Command.ChatMessage.UserType,
						StandardMessages.PollMessages.PollDefaultFormat, StandardMessages.PollMessages.PollModFormat);
			}
		}


		private string PollCreateCommand(string pollOptionsString)
		{
			if (string.IsNullOrEmpty(pollOptionsString) || pollOptionsString.Contains("||"))
				return StandardMessages.PollMessages.PollCreateFormat;

			var options = CommandHelpers.FilterAndPreparePollOptions(pollOptionsString);
			if (options.Count < 2) return StandardMessages.PollMessages.PollCreateFormat;

			var creationSuccess = _pollManager.PollCreate(options);
			if (!creationSuccess)
			{
				Log.Error("Something major failed when creating the poll {paramString}", pollOptionsString);
				return StandardMessages.ErrorMessages.BigError;
			}

			var resultItems = _pollManager.PollStats();
			var builder = new StringBuilder();
			builder.Append("Poll Creat! Optiuni: ");
			for (var i = 0; i < resultItems.Count; i++) builder.AppendFormat(" //{0}:{1}", i + 1, resultItems[i].Name);
			PollOptionsString = CommandHelpers.OptionsStringBuilder(_pollManager.PollStats().Count);
			return $"/me {builder}";
		}

		private async Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e)
		{
			if (e.Command.ArgumentsAsList.Count <= 1 || !int.TryParse(e.Command.ArgumentsAsList[1], out var votedNumber))
				return StandardMessages.PollMessages.PollVoteNotNumber;
			var voteState = await _pollManager.PollAddVoteAsync(e.Command.ChatMessage.UserId, votedNumber)
				.ConfigureAwait(false);

			switch (voteState)
			{
				case PollAddVoteFinishState.PollNotActive:
					return StandardMessages.PollMessages.PollNotActive;
				case PollAddVoteFinishState.VoteAdded:
					return
						$"/me {e.Command.ChatMessage.DisplayName} a votat pentru '{_pollManager.PollItems[votedNumber - 1].Name}'";
				case PollAddVoteFinishState.OptionOutOfRange:
					if (PollOptionsString != null) return $"/me Foloseste !poll vote {PollOptionsString}";
					Log.Error("PollOptionsString shouldn't be null when vote is out of range... returning null!");
					return "/me Foloseste !poll vote ERROR: LIPSESC OPTIUNILE. SEND LOGS.";
				case PollAddVoteFinishState.VoteFailed:
					Log.Error("Vote failed for {DisplayName}, chat message: {message}",
						e.Command.ChatMessage.DisplayName, e.Command.ChatMessage.Message);
					return
						$"/me {e.Command.ChatMessage.DisplayName} votul tau a esuat. Te rog contacteaza un moderator!";
				default:
					return null;
			}
		}

		private string PollStatsCommand()
		{
			var resultItems = _pollManager.PollStats();
			if (resultItems == null)
				return StandardMessages.PollMessages.PollNotActive;

			var builder = new StringBuilder();
			builder.Append("Statistici :");
			for (var i = 0; i < resultItems.Count; i++)
				builder.AppendFormat(" //{0}:{1}", resultItems[i].Name, resultItems[i].Points);
			return $"/me {builder}";
		}

		private string PollEndCommand()
		{
			var resultItem = _pollManager.PollEnd();
			if (resultItem == null)
				return StandardMessages.PollMessages.PollNotActive;
			PollOptionsString = null;
			var message = $"A Castigat || {resultItem.Name} || cu {resultItem.Points} puncte";
			return $"/me {message}";
		}
	}
}
