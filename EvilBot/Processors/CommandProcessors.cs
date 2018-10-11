﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.Processors.Interfaces;
using EvilBot.Utilities;
using EvilBot.Utilities.Interfaces;
using TwitchLib.Client.Events;

namespace EvilBot.Processors
{
    internal class CommandProcessor : ICommandProcessor
    {
        private readonly IDataProcessor _dataProcessor;
        private readonly IDataAccess _dataAccess;
        private readonly IPollManager _pollManager;
        private readonly IFilterManager _filterManager;

        public CommandProcessor(IDataProcessor dataProcessor, IDataAccess dataAccess, IPollManager pollManager, IFilterManager filterManager)
        {
            _dataProcessor = dataProcessor;
            _dataAccess = dataAccess;
            _pollManager = pollManager;
            _filterManager = filterManager;
        }

        public async Task<string> RankCommandAsync(OnChatCommandReceivedArgs e)
        {
            if (string.IsNullOrEmpty(e.Command.ArgumentsAsString))
            {
                var results = await _dataProcessor.GetUserAttributesAsync(e.Command.ChatMessage.UserId).ConfigureAwait(false);
                if (results != null)
                {
                    return $"/me {e.Command.ChatMessage.DisplayName} esti {_dataProcessor.GetRankFormatted(results[2], results[0])} cu {Math.Round(double.Parse(results[1], System.Globalization.CultureInfo.InvariantCulture) / 60, 1)} ore!\n\r";
                }
                return $"/me {e.Command.ChatMessage.DisplayName} You aren't yet in the database. You'll be added at the next minute check!";
            }
            else
            {
                var results = await _dataProcessor.GetUserAttributesAsync(await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsString.TrimStart('@').ToLower()).ConfigureAwait(false)).ConfigureAwait(false);
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
            if (string.IsNullOrEmpty(e.Command.ArgumentsAsString)) return StandardMessages.ManageCommandText;
            if (e.Command.ArgumentsAsList.Count < 2 || (userid = await _dataProcessor.GetUserIdAsync(e.Command.ArgumentsAsList[0].TrimStart('@')).ConfigureAwait(false)) == null) 
                return StandardMessages.ManageCommandText;
            var pointModifier = 0;
            var minuteModifier = 0;
            var twoParams = false;
            var parameters = new List<string> { e.Command.ArgumentsAsList[1] };
            if (e.Command.ArgumentsAsList.Count == 3)
            {
                twoParams = true;
                parameters = CommandHelpers.ManageCommandSorter(e.Command.ArgumentsAsList[1], e.Command.ArgumentsAsList[2]);
            }
            if (parameters[0].EndsWith("m", StringComparison.InvariantCultureIgnoreCase))
            {
                parameters[0] = parameters[0].TrimEnd('m', 'M');

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
            await _dataAccess.ModifierUserIdAsync(userid, pointModifier, minuteModifier).ConfigureAwait(false);
            return $"/me Modified {e.Command.ArgumentsAsList[0]} with {pointModifier} points and {minuteModifier} minutes";
        }

        #region PollCommands

        public string PollCreateCommand(OnChatCommandReceivedArgs e)
        {
            if (string.IsNullOrEmpty(e.Command.ArgumentsAsString) || e.Command.ArgumentsAsString.Contains("||"))
                return StandardMessages.PollCreateText;
            var arguments = e.Command.ArgumentsAsString.Trim();
            arguments = arguments.Trim('|');
            arguments = arguments.Trim();
            var options = arguments.Split('|').ToList();
            for (var i = 0; i < options.Count; i++)
            {
                options[i] = options[i].Trim();
            }
            if (options.Count >= 2)
            {
                return $"/me {_pollManager.PollCreate(options)}";
            }
            return StandardMessages.PollCreateText;
        }

        public async Task<string> PollVoteCommandAsync(OnChatCommandReceivedArgs e)
        {
            if (!_pollManager.PollActive) return StandardMessages.PollNotActiveText;
            if (!int.TryParse(e.Command.ArgumentsAsString, out int votedNumber)) return StandardMessages.PollVoteText;
            await _pollManager.PollAddVote(e.Command.ChatMessage.UserId, votedNumber).ConfigureAwait(false);
            return null;
        }

        public string PollStatsCommand(OnChatCommandReceivedArgs e)
        {
            return _pollManager.PollActive ? $"/me {_pollManager.PollStats()}" : StandardMessages.PollNotActiveText;
        }

        public string PollEndCommand(OnChatCommandReceivedArgs e)
        {
            return _pollManager.PollActive ? $"/me {_pollManager.PollEnd()}" : StandardMessages.PollNotActiveText;
        }

        #endregion PollCommands

        #region FilterCommands

        public async Task<string> FilterCommand(OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsList.Count >= 1 && e.Command.ArgumentsAsList[0] == "get")
            {
                return _filterManager.RetrieveFilteredUsers();
            }
            if (e.Command.ArgumentsAsList.Count < 2) return StandardMessages.FilterText;
            switch (e.Command.ArgumentsAsList[0])
            {
                case "add":
                {
                    var user = await _dataProcessor.GetUserAsyncByUsername(e.Command.ArgumentsAsList[1]);
                    if (user == null) return StandardMessages.UserMissingText;
                    await _filterManager.AddToFiler(new UserBase(user.DisplayName, user.Id));
                    //TODO make later to show different text if user already in filter or not
                    return $"/me {user.DisplayName} adaugat la Filtru!";
                }
                case "remove":
                {
                    var user = await _dataProcessor.GetUserAsyncByUsername(e.Command.ArgumentsAsList[1]);
                    if (user == null) return StandardMessages.UserMissingText;
                    if (await _filterManager.RemoveFromFilter(new UserBase(user.DisplayName, user.Id)))
                    {
                        return $"/me {user.DisplayName} sters din Filtru!";
                    }
                    return $"/me {user.DisplayName} nu este in Filtru!";
                }
                default:
                    return StandardMessages.FilterText;
            }
        }

        #endregion
    }
}