using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EvilBot.DataStructures;
using EvilBot.DataStructures.Interfaces;
using EvilBot.EventArguments;
using EvilBot.Managers.Interfaces;
using EvilBot.Resources;
using EvilBot.Resources.Interfaces;
using Serilog;

namespace EvilBot.Managers
{
	public class RankManager : IRankManager
	{
		private readonly List<Tuple<string, int>> _ranks = new List<Tuple<string, int>>();
		private readonly IDataAccess _dataAccess;


		public RankManager(IDataAccess dataAccess)
		{
			_dataAccess = dataAccess;
			InitializeRanks();
		}

		public event EventHandler<RankUpdateEventArgs> RankUpdated;

		protected virtual void OnRankUpdated(string name, string rank)
		{
			RankUpdated?.Invoke(this, new RankUpdateEventArgs {Name = name, Rank = rank});
		}

		private void InitializeRanks()
		{
			_ranks.Add(new Tuple<string, int>("Fara Rank", 0));
			_ranks.Add(new Tuple<string, int>("Rookie", 50));
			_ranks.Add(new Tuple<string, int>("Alpha", 500));
			_ranks.Add(new Tuple<string, int>("Thug", 2500));
			_ranks.Add(new Tuple<string, int>("Sage", 6000));
			_ranks.Add(new Tuple<string, int>("Lord", 10000));
			_ranks.Add(new Tuple<string, int>("Initiate", 15000));
			_ranks.Add(new Tuple<string, int>("Veteran", 22000));
			_ranks.Add(new Tuple<string, int>("Emperor", 30000));
		}


		public string GetRankFormatted(string rankString, string pointsString)
		{
			if (int.TryParse(rankString, out var rank) && int.TryParse(pointsString, out var points))
			{
				if (rank == 0) return $"{_ranks[rank].Item1} XP: {points}/{_ranks[rank + 1].Item2}";
				if (rank == _ranks.Count - 1) return $"{_ranks[rank].Item1} (Lvl.{rank}) XP: {points}";
				return $"{_ranks[rank].Item1} (Lvl.{rank}) XP: {points}/{_ranks[rank + 1].Item2}";
			}

			Log.Error("{rankString} {pointsString} is not a parseable value to int {method}", rankString, pointsString,
				$"{ToString()} GetRankFormatted");
			return null;
		}

		public int GetRank(int points)
		{
			var place = 0;
			for (var i = 0; i < _ranks.Count - 1; i++)
			{
				if (points < _ranks[i + 1].Item2) break;
				place = i + 1;
			}

			return place;
		}


		public async Task UpdateRankAsync(IReadOnlyList<IUserBase> userList)
		{
			Log.Debug("Checking Ranks for {userCount}", userList.Count);
			var usersUpdated = new List<IUserStructure>();
			var databaseRankUpdateTasks = new List<Task>();
			var getUserAttributesTasks = userList
				.Select(t => _dataAccess.RetrieveUserFromTableAsync(Enums.DatabaseTables.UserPoints, t.UserId))
				.ToList();
			var userAttributes = (await Task.WhenAll(getUserAttributesTasks).ConfigureAwait(false)).ToList();
			if (userAttributes.Contains(null))
				Log.Error(
					"There shouldn't have been sent a null user or every user should have been in the database. INVESTIGATE!");
			userAttributes.RemoveAll(x => x == null);
			var query =
				from userAttribute in userAttributes
				join user in userList on userAttribute.UserId equals user.UserId
				select new UserStructureData(user.DisplayName, userAttribute.Id, userAttribute.UserId,
					userAttribute.Points, userAttribute.Minutes, userAttribute.Rank);
			var users = query.ToList<IUserStructure>();
			for (var i = 0; i < users.Count; i++)
				try
				{
					if (!int.TryParse(users[i].Points, out var points))
					{
						Log.Error("Tried to parse string to int: {string} in {ClassSource}", users[i].Points,
							$"{ToString()}UpdateRankAsync");
						continue;
					}

					if (!int.TryParse(users[i].Rank, out var rank))
					{
						Log.Error("Tried to parse string to int: {string} in {ClassSource}", users[i].Rank,
							$"{ToString()}UpdateRankAsync");
						continue;
					}

					var currentRank = GetRank(points);
					if (currentRank == rank) continue;
					databaseRankUpdateTasks.Add(_dataAccess.ModifyUserIdRankAsync(userList[i].UserId, currentRank));
					users[i].Rank = currentRank.ToString();
					usersUpdated.Add(users[i]);
				}
				catch (NullReferenceException e)
				{
					Log.Error(e,
						"Some null happened, probably userAttributes, prevent from sending the null onward, or check why it was sent {problemSource}",
						e.Source);
					await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
					throw;
				}
				catch (Exception e)
				{
					Log.Error(e,
						"Ok, this is pretty bad, it's not null so somebody didn't know how to handle stuff {source}",
						e.Source);
					await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
					throw;
				}

			await Task.WhenAll(databaseRankUpdateTasks).ConfigureAwait(false);
			for (var i = 0; i < usersUpdated.Count; i++)
				OnRankUpdated(usersUpdated[i].DisplayName,
					$"{_ranks[int.Parse(usersUpdated[i].Rank)].Item1} (Lvl. {usersUpdated[i].Rank})");
		}

		public List<IRankItem> GetRankList()
		{
			return _ranks.Select((t, i) => new RankItem(i, t.Item1, t.Item2)).ToList<IRankItem>();
		}
	}
}
