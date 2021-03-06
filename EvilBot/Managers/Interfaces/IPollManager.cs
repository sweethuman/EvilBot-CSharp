﻿using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Resources.Enums;

namespace EvilBot.Managers.Interfaces
{
	public interface IPollManager
	{
		bool PollActive { get; }
		List<IPollItem> PollItems { get; }

		Task<PollAddVoteFinishState> PollAddVoteAsync(string userId, int votedNumber);

		/// <summary>
		///     Initializes a new poll
		/// </summary>
		/// <param name="optionsList">Text values of PollItems</param>
		/// <returns>Creation success state</returns>
		bool PollCreate(List<string> optionsList);

		IPollItem PollEnd();

		List<IPollItem> PollStats();
	}
}