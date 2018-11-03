using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using EvilBot.DataStructures.Interfaces;
using EvilBot.Utilities;

namespace EvilBot.Processors.Interfaces
{
	public interface IDataProcessor
	{
		event EventHandler<RankUpdateEventArgs> RankUpdated;

		Task<List<string>> GetUserAttributesAsync(string userId);

		string GetRankFormatted(string rankString, string pointsString);

		Task AddToUserAsync(List<IUserBase> userList, int points = 1, int minutes = 0, bool subCheck = true);

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.

		void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

		void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
	}
}