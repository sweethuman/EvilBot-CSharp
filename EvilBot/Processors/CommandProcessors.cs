using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client.Events;

namespace EvilBot.Processors
{
    internal class CommandProcessor : ICommandProcessor
    {
        private IDataProcessor _dataProcessor;
        private IDataAccess _dataAccess;
        private IPollManager _pollManager;

        public CommandProcessor(IDataProcessor dataProcessor, IDataAccess dataAccess, IPollManager pollManager)
        {
            _dataProcessor = dataProcessor;
            _dataAccess = dataAccess;
            _pollManager = pollManager;
        }

        public async Task<string> RankCommandAsync(OnChatCommandReceivedArgs e)
        {
            if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
            {
                List<string> results = await _dataProcessor.GetUserAttributesAsync(e.Command.ChatMessage.UserId).ConfigureAwait(false);
                if (results != null)
                {
                    return $"/me {e.Command.ChatMessage.DisplayName} esti {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!\n\r";
                }
                return $"/me {e.Command.ChatMessage.DisplayName} You aren't yet in the database. You'll be added at the next minute check!";
            }
            else
            {
                List<string> results = await _dataProcessor.GetUserAttributesAsync(await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false)).ConfigureAwait(false);
                if (results != null)
                {
                    return $"/me {e.Command.ArgumentsAsString.TrimStart('@')} este {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!";
                }
                return $"/me {e.Command.ArgumentsAsString.TrimStart('@')} isn't yet in the database!";
            }
        }

        public async Task<string> ManageCommandAsync(OnChatCommandReceivedArgs e)
        {
            string userid;
            if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString))
            {
                if (!(e.Command.ArgumentsAsList.Count < 2) && (userid = await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@')).ConfigureAwait(false)) != null)
                {
                    int pointModifier = 0;
                    int minuteModifier = 0;
                    bool twoParams = false;
                    List<string> parameters = new List<string>() { e.Command.ArgumentsAsList[1] };
                    if (e.Command.ArgumentsAsList.Count == 3)
                    {
                        twoParams = true;
                        parameters = CommandHelpers.ManageCommandSorter(e.Command.ArgumentsAsList[1], e.Command.ArgumentsAsList[2]);
                    }
                    if (parameters[0].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
                    {
                        parameters[0] = parameters[0].TrimEnd(new char[] { 'm', 'M' });

                        if (!int.TryParse(parameters[0], out minuteModifier))
                        {
                            return StandardMessages.ManageCommandText;
                        }
                    }
                    else
                    {
                        if (!int.TryParse(parameters[0], out pointModifier))
                        {
                            return StandardMessages.ManageCommandText;
                        }
                    }

                    if (twoParams)
                    {
                        if (parameters[1].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
                        {
                            parameters[1] = parameters[1].TrimEnd(new char[] { 'm', 'M' });

                            if (!int.TryParse(parameters[1], out minuteModifier))
                            {
                                return StandardMessages.ManageCommandText;
                            }
                        }
                        else
                        {
                            return StandardMessages.ManageCommandText;
                        }
                    }
                    await _dataAccess.ModifierUserIDAsync(userid, pointModifier, minuteModifier).ConfigureAwait(false);
                    return $"/me Modified {e.Command.ArgumentsAsList[0]} with {pointModifier} points and {minuteModifier} minutes";
                }
                return StandardMessages.ManageCommandText;
            }
            return StandardMessages.ManageCommandText;
        }

        #region PollCommands

        public string PollCreateCommand(OnChatCommandReceivedArgs e)
        {
            if (!string.IsNullOrEmpty(e.Command.ArgumentsAsString) && !e.Command.ArgumentsAsString.Contains("||"))
            {
                string arguments = e.Command.ArgumentsAsString.Trim();
                arguments = arguments.Trim('|');
                arguments = arguments.Trim();
                List<string> options = arguments.Split('|').ToList();
                for (int i = 0; i < options.Count; i++)
                {
                    options[i] = options[i].Trim();
                }
                if (!(options.Count < 2))
                {
                    return $"/me {_pollManager.PollCreate(options)}";
                }
                return StandardMessages.PollCreateText;
            }
            return StandardMessages.PollCreateText;
        }

        public async Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e)
        {
            if (_pollManager.PollActive)
            {
                if (int.TryParse(e.Command.ArgumentsAsString, out int votedNumber))
                {
                    await _pollManager.PollAddVote(e.Command.ChatMessage.UserId, votedNumber).ConfigureAwait(false);
                    return null;
                }
                return StandardMessages.PollVoteText;
            }
            return StandardMessages.PollNotActiveText;
        }

        public string PollStatsCommand(OnChatCommandReceivedArgs e)
        {
            if (_pollManager.PollActive)
            {
                return $"/me {_pollManager.PollStats()}";
            }
            return StandardMessages.PollNotActiveText;
        }

        public string PollEndCommand(OnChatCommandReceivedArgs e)
        {
            if (_pollManager.PollActive)
            {
                return $"/me {_pollManager.PollEnd()}";
            }
            return StandardMessages.PollNotActiveText;
        }

        #endregion PollCommands
    }
}