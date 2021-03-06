using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;

namespace EvilBot.Managers.Interfaces
{
	public interface IRankManager
	{
		string RankListString { get; }
		event EventHandler<RankUpdateEventArgs> RankUpdated;
		string GetRankFormatted(string rankString, string pointsString);
		int CalculateRank(int points);
		List<IRankItem> GetRankList();
		IRankItem GetRank(int rank);
		Task UpdateRankAsync(IUserBase user);
		Task UpdateRankAsync(IEnumerable<string> userIds);
		Task UpdateRankAsync(IReadOnlyList<IUserBase> userList);
	}
}
