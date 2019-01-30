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
		int GetRank(int points);
		Task UpdateRankAsync(IReadOnlyList<IUserBase> userList);
		List<IRankItem> GetRankList();
	}
}