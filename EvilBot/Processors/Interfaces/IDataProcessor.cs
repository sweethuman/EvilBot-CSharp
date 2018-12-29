using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;
using EvilBot.DataStructures.Interfaces;

namespace EvilBot.Processors.Interfaces
{
	public interface IDataProcessor
	{

		/// <summary>
		///     Adds Points to the Users asynchronously.
		/// </summary>
		/// <param name="userList">The users to add to the defined values.</param>
		/// <param name="points">The points to add.</param>
		/// <param name="minutes">The minutes to add.</param>
		/// <param name="subCheck">If set to <c>true</c> it will check if users are subscribers.</param>
		/// <returns>Just a task.</returns>
		Task AddToUserAsync(List<IUserBase> userList, int points = 1, int minutes = 0, bool subCheck = true);

#pragma warning disable RCS1047 // Non-asynchronous method name should not end with 'Async'.

		void AddLurkerPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

		void AddPointsTimer_ElapsedAsync(object sender, ElapsedEventArgs e);

#pragma warning restore RCS1047 // Non-asynchronous method name should not end with 'Async'.
		List<T> RemoveFilteredUsers<T>(List<T> userList) where T : IUserBase;
	}
}
